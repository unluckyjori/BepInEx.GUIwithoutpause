// Comment for enabling console
#![windows_subsystem = "windows"]

use config::launch::AppLaunchConfig;
use eframe::egui::*;
use std::env;

mod app;
mod backend;
mod config;
mod data;
mod logger;
mod theme;
mod views;

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

        icon_data: Some(load_icon(icon_path.into())),

        ..Default::default()
    };

    match eframe::run_native(
        app::NAME,
        native_options,
        Box::new(|cc| Box::new(gui.init(cc))),
    ) {
        Ok(_) => {}
        Err(res) => tracing::error!("{:?}", res),
    }
}

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
