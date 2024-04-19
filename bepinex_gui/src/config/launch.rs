use std::{path::PathBuf, u16};

use sysinfo::Pid;

use crate::app;

const DEBUG: bool = false;
const DEFAULT_PID: usize = 17584;
const DEFUALT_SOCKET_PORT: u16 = 27090;

pub struct AppLaunchConfig {
    launch_info: AppLaunchArgsInfo,
    window_title: String,
}

pub struct AppLaunchArgsInfo {
    pub bepinex_version: String,       //arg [1]
    pub process_name: String,          //arg [2]
    pub game_folder_path: PathBuf,     //arg [3]
    pub log_output_file_path: PathBuf, //arg [4]
    pub gui_cfg_full_path: PathBuf,    //arg [5]
    pub target_process_id: Pid,        //arg [6]
    pub log_socket_port: u16,          //arg [7]
    pub icon_path: String,             //arg [8]
}

impl Default for AppLaunchArgsInfo {
    fn default() -> Self {
        if DEBUG {
            let (debug_log_socket_port_receiver, debug_target_process_id, debug_icon_path) =
                (51730 as u16, Pid::from(24988), String::from("None"));
            return Self {
                process_name: "Lethal Company".into(),
                bepinex_version: "5.4.2100".into(),
                game_folder_path: "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Lethal Company".into(),
                log_output_file_path: "C:\\Program Files (x86)\\r2modmanPlus-local\\LethalCompany\\profiles\\Default\\BepInEx\\LogOutput.log".into(),
                gui_cfg_full_path: "C:\\Program Files (x86)\\r2modmanPlus-local\\LethalCompany\\profiles\\Default\\BepInEx\\config\\BepInEx.GUI.cfg".into(),
                target_process_id: debug_target_process_id.into(),
                log_socket_port: debug_log_socket_port_receiver.into(),
                icon_path: debug_icon_path.into(),
            };
        }
        return Self {
            process_name: "BepInEx Console GUI".into(),
            bepinex_version: "3.1.0".into(),
            game_folder_path: "C:\\Program Files (x86)".into(),
            log_output_file_path: "C:\\Program Files (x86)".into(),
            gui_cfg_full_path: "C:\\Program Files (x86)".into(),
            target_process_id: DEFAULT_PID.into(),
            log_socket_port: DEFUALT_SOCKET_PORT.into(),
            icon_path: "None".into(),
        };
    }
}

impl AppLaunchConfig {
    const ARG_COUNT: usize = 8;

    pub fn from(args: &Vec<String>) -> Option<Self> {
        if args.len() < Self::ARG_COUNT {
            tracing::error!("Problem with args {:?} {:?}", args.len(), args);
            return None;
        }

        let launch_info = AppLaunchArgsInfo {
            bepinex_version: (&args[1]).into(),
            process_name: (&args[2]).into(),
            game_folder_path: (&args[3]).into(),
            log_output_file_path: (&args[4]).into(),
            gui_cfg_full_path: (&args[5]).into(),
            target_process_id: args[6].parse::<Pid>().unwrap(),
            log_socket_port: args[7].parse::<u16>().unwrap(),
            icon_path: match args.len() > Self::ARG_COUNT {
                true => String::from(&args[8]),
                false => String::from("None"),
            },
        };
        let window_title = format!(
            "{} {} - {}",
            app::NAME.to_owned(),
            launch_info.bepinex_version,
            launch_info.process_name
        );

        return Some(Self {
            launch_info: launch_info,
            window_title: window_title,
        });
    }

    ///no reason to defualt to RoR2 as nor steam in the C Drive as you cant be sure they even have it installed.
    ///tho you can be pretty sure.
    pub fn default() -> Self {
        let launch_info = AppLaunchArgsInfo::default();
        let window_title = format!(
            "{} {} - {}",
            app::NAME.to_owned(),
            launch_info.bepinex_version,
            launch_info.process_name
        );
        Self {
            launch_info: launch_info,
            window_title: window_title,
        }
    }

    #[allow(dead_code)]
    pub fn launch_info(&self) -> &AppLaunchArgsInfo {
        &self.launch_info
    }

    pub fn process_name(&self) -> &str {
        &self.launch_info.process_name
    }

    pub fn game_folder_path(&self) -> &PathBuf {
        &self.launch_info.game_folder_path
    }

    pub fn log_output_file_path(&self) -> &PathBuf {
        &self.launch_info.log_output_file_path
    }

    pub fn gui_cfg_full_path(&self) -> &PathBuf {
        &self.launch_info.gui_cfg_full_path
    }

    pub const fn target_process_id(&self) -> Pid {
        self.launch_info.target_process_id
    }

    pub const fn log_socket_port(&self) -> u16 {
        self.launch_info.log_socket_port
    }

    pub fn icon_path(&self) -> &str {
        &self.launch_info.icon_path
    }

    pub fn window_title(&self) -> &str {
        &self.window_title
    }
}
