use core::time;
use crossbeam_channel::Sender;
use std::net::IpAddr;
use std::net::Ipv4Addr;
use std::net::SocketAddr;
use std::net::TcpStream;
use std::thread;

use crate::backend::network::packet_protocol;
use crate::data::bepinex_mod::BepInExMod;

use super::BepInExLogEntry;
use super::LogLevel;

#[derive(Clone)]
pub struct LogReceiver {
    log_socket_port_receiver: u16,
    log_senders: Vec<Sender<BepInExLogEntry>>,
    mod_senders: Vec<Sender<BepInExMod>>,
}

impl LogReceiver {
    pub fn new(
        log_socket_port_receiver: u16,
        log_senders: Vec<Sender<BepInExLogEntry>>,
        mod_senders: Vec<Sender<BepInExMod>>,
    ) -> Self {
        Self {
            log_socket_port_receiver,
            log_senders,
            mod_senders,
        }
    }

    pub fn start_thread_loop(&self) {
        let server_address = SocketAddr::new(
            IpAddr::V4(Ipv4Addr::new(127, 0, 0, 1)),
            self.log_socket_port_receiver,
        );
        let inst = self.clone();
        thread::spawn(move || Self::thread_loop(inst, server_address, 5, 10));
    }

    fn thread_loop(
        inst: LogReceiver,
        server_address: SocketAddr,
        num_of_connection_attempts: u32,
        num_of_read_packet_attempts: u32,
    ) {
        let mut connection_attempts = 0;
        loop {
            let tcp_stream = TcpStream::connect(server_address);
            if let Err(tcp_stream) = tcp_stream {
                tracing::error!("Failed connecting: {}", tcp_stream);
                connection_attempts += 1;
                match connection_attempts >= num_of_connection_attempts {
                    true => break,
                    false => continue,
                }
            }
            let mut read_packet_attempts = 0;
            let mut tcp_stream =
                tcp_stream.expect("Failed connecting after check somehow. what how?");
            loop {
                let packet_lenght = packet_protocol::read_packet_length(&mut tcp_stream);
                if let Err(packet_lenght) = packet_lenght {
                    tracing::error!(
                        "Error reading packet length: {}\nDisconnecting socket",
                        packet_lenght
                    );
                    read_packet_attempts += 1;
                    match read_packet_attempts >= num_of_read_packet_attempts {
                        true => break,
                        false => continue,
                    }
                }

                let packet_length = packet_lenght
                    .expect("Error reading packet length after check somehow. what how?");
                let log_level = packet_protocol::read_packet_log_level(&mut tcp_stream);
                if let Err(log_level) = log_level {
                    tracing::error!(
                        "Error reading packet log level: {}\nDisconnecting socket",
                        log_level
                    );
                    read_packet_attempts += 1;
                    match read_packet_attempts >= num_of_read_packet_attempts {
                        true => break,
                        false => continue,
                    }
                }
                let log_level = log_level
                    .expect("Error reading packet log level after check somehow. what how?");
                let packet_bytes = packet_protocol::read_packet(&mut tcp_stream, packet_length);
                if let Err(packet_bytes) = packet_bytes {
                    tracing::error!(
                        "Error reading packet: {}\nDisconnecting socket",
                        packet_bytes
                    );
                    read_packet_attempts += 1;
                    match read_packet_attempts >= num_of_read_packet_attempts {
                        true => break,
                        false => continue,
                    }
                }
                let packet_bytes =
                    packet_bytes.expect("Error reading packet after check somehow. what how?");
                inst.make_log_entry_from_packet_data(log_level, &packet_bytes);
            }
        }
        const DELAY_IN_MS_BETWEEN_CONNECTION_TRY: u64 = 2000;
        thread::sleep(time::Duration::from_millis(
            DELAY_IN_MS_BETWEEN_CONNECTION_TRY,
        ));
    }
    fn make_log_entry_from_packet_data(&self, log_level: LogLevel, string_packet_bytes: &[u8]) {
        let log_string = packet_protocol::packet_bytes_to_utf8_string(string_packet_bytes);

        let log = BepInExLogEntry::new(log_level, &log_string);

        if log.data().contains("Loading [") {
            let split: Vec<&str> = log.data().split('[').collect();
            let mod_info_text = split[2];
            let mod_version_start_index_ = mod_info_text.rfind(' ');
            if let Some(mod_version_start_index) = mod_version_start_index_ {
                let mod_name = &mod_info_text[0..mod_version_start_index];
                let mod_version =
                    &mod_info_text[mod_version_start_index + 1..mod_info_text.len() - 1];

                for mod_sender in &self.mod_senders {
                    mod_sender
                        .send(BepInExMod::new(mod_name, mod_version))
                        .unwrap();
                }
            }
        }

        for log_sender in &self.log_senders {
            log_sender.send(log.clone()).unwrap();
        }
    }
}
