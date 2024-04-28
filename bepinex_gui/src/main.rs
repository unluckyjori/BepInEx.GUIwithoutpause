// Comment for enabling console
#![windows_subsystem = "windows"]

use config::launch::AppLaunchConfig;
use eframe::egui::*;
use std::env;
//use std::{borrow::BorrowMut, future::IntoFuture, io::Write, thread};
//use tracing_tunnel::{TracingEventReceiver, TracingEventSender};

mod app;
mod backend;
mod config;
mod data;
mod logger;
mod theme;
mod views;

//use flume::{Receiver, Sender};
//use tracing_tunnel::TracingEvent;

fn main() {
    logger::init();
    backend::init();
    let args: Vec<String> = env::args().collect();

    let app_config = AppLaunchConfig::from(&args).unwrap_or_else(AppLaunchConfig::default);
    let icon_path = app_config.icon_path().to_owned();
    let gui = app::BepInExGUI::new(app_config);
    let native_options = eframe::NativeOptions {
        min_window_size: Some(Vec2::new(480., 270.)),
        initial_window_size: Some(Vec2::new(1034., 520.)),
        initial_centered: true,
        icon_data: Some(load_icon(icon_path.clone())),

        ..Default::default()
    };
    tracing::error!("{:?}",icon_path);
    match eframe::run_native(
        app::NAME,
        native_options,
        Box::new(|cc| Box::new(gui.init(cc))),
    ) {
        Ok(_) => {}
        Err(res) => tracing::error!("{:?}", res),
    }
}

// fn init_log(){
//     tracing_subscriber::fmt::init();
//     let (events_sx, events_rx): (Sender<TracingEvent>, Receiver<TracingEvent>) = flume::unbounded();
//     let subscriber = TracingEventSender::new(move |event| {
//         let _ = events_sx.send(event).ok();
//     });
//     let _ = tracing::subscriber::set_global_default(subscriber);
//     let mut receiver = TracingEventReceiver::default();
//     log_reciver(events_rx, receiver);
// }
// async fn log_reciver(receiver: Receiver<TracingEvent>, mut tracing_reciever: TracingEventReceiver) {
//     thread::spawn(move || async {
//     let mut foo = receiver.into_recv_async();
//         loop {
//             let mut boo = foo.borrow_mut().into_future();
//             let  a = boo.await;
//             if let Ok(log) = a {
                
//             }
//         }
//     });
// }


// fn communicate_wrapper() {
//     let estr = "Hello World from Error std out in rust\n"
//         .as_bytes()
//         .to_vec();
//     let ostr = "Hello World from Out std out in rust\n".as_bytes().to_vec();

//     let mut err = std::io::stderr();
//     let mut out = std::io::stdout();

//     if let Ok(_a) = err.write(estr.as_slice()) {
//         let _ = err.flush();
//     }

//     if let Ok(_b) = out.write(ostr.as_slice()) {
//         let _ = out.flush();
//     }

//     let success = "sucess from rust\n".as_bytes().to_vec();
//     let failure = "failure from rust\n".as_bytes().to_vec();

//     let foo = communicate();
//     if let Ok(_) = foo {
//         if let Ok(_a) = err.write(success.as_slice()) {
//             let _ = err.flush();
//         }

//         if let Ok(_b) = out.write(success.as_slice()) {
//             let _ = out.flush();
//         }
//     } else {
//         if let Ok(_a) = err.write(failure.as_slice()) {
//             let _ = err.flush();
//         }

//         if let Ok(_b) = out.write(failure.as_slice()) {
//             let _ = out.flush();
//         }
//     }
// }

// pub type Error = Box<dyn std::error::Error + Send + Sync>;
// pub type Result<T> = std::result::Result<T, Error>;
// fn communicate() -> Result<()> {
//     use std::{io, io::prelude::*};

//     for line in io::stdin().lock().lines() {
//         println!("length = {}", line?.len());
//     }
//     Ok(())
// }

fn load_icon(app_icon: String) -> eframe::IconData {
    if app_icon != "None" {
        let icon = image::open(app_icon);
        if let Ok(icon) = icon {
            let image = icon.into_rgba8();
            let (width, height, rgba) = { (image.width(), image.height(), image.into_raw()) };

            return eframe::IconData {
                width: width,
                height: height,
                rgba: rgba,
            };
        }
    }

    let icon = include_bytes!("../assets/icons/discord_server_icon.png");
    let image = image::load_from_memory(icon).expect("Failed to open icon from the given path");
    let image = image.to_rgba8();
    let (width, height, rgba) = { (image.width(), image.height(), image.into_raw()) };
    return eframe::IconData {
        width: width,
        height: height,
        rgba: rgba,
    };
}
