use std::{
    os::windows::process::CommandExt,
    path::{Path, PathBuf},
    process::Command,
};

// Windows
#[cfg(target_os = "windows")]
pub fn open_path_in_explorer(file: &PathBuf) {
    if let Err(e) = (|| {
        #[cfg(target_os = "windows")]
        return Command::new("explorer").arg(file).spawn();
    })() {
        tracing::error!("{}", e)
    }
}

#[cfg(target_os = "windows")]
pub fn highlight_path_in_file_explorer(file: &Path) {
    if let Err(e) = (|| {
        let s = file.to_str();
        if let Some(s) = s {
            let mut s = s.replace('/', r#"\"#);
            s.push('\"');
            let s = s.as_str();
            return Command::new("explorer")
                .raw_arg("/select,\"".to_string() + s)
                .spawn();
        }
        return Err(std::io::Error::new(
            std::io::ErrorKind::Other,
            "Can't convert PathBuf to_str",
        ));
    })() {
        tracing::error!("{}", e)
    }
}
// #Windows

// Mac os
#[cfg(target_os = "macos")]
pub fn open_path_in_explorer(file: &PathBuf) {
    if let Err(e) = (|| {
        return Command::new("open").arg(path.to_string()).spawn();
    })() {
        tracing::error!("{}", e)
    }
}

#[cfg(target_os = "macos")]
pub fn highlight_path_in_explorer(file: &Path) {
    if let Err(e) = (|| {
        return Command::new("open").arg("-R").arg(path.to_string()).spawn();
    })() {
        tracing::error!("{}", e)
    }
}
// #Mac os

// Linux
#[cfg(target_os = "linux")]
pub fn open_path_in_explorer(file: &PathBuf) {
    if let Err(e) = (|| {
        return Command::new("xdg-open").arg(path.to_string()).spawn();
    })() {
        tracing::error!("{}", e)
    }
}

#[cfg(target_os = "linux")]
pub fn highlight_path_in_explorer(file: &Path) {
    if let Err(e) = (|| {
        return Command::new("xdg-open")
            .arg("--select")
            .arg(path.to_string())
            .spawn();
    })() {
        tracing::error!("{}", e)
    }
}
// #Linux

// Unsupported operating systems
#[cfg(not(target_os = "windows"))]
#[cfg(not(target_os = "macos"))]
#[cfg(not(target_os = "linux"))]
pub fn open_path_in_explorer(file: &PathBuf) {
    if let Err(e) = (|| {
        Err(std::io::Error::new(
            std::io::ErrorKind::Other,
            "Unsupported OS",
        ))
    })() {
        tracing::error!("{}", e);
    }
}

#[cfg(not(target_os = "windows"))]
#[cfg(not(target_os = "macos"))]
#[cfg(not(target_os = "linux"))]
pub fn highlight_path_in_explorer(file: &Path) {
    if let Err(e) = (|| {
        return Err(std::io::Error::new(
            std::io::ErrorKind::Other,
            "Unsupported OS",
        ));
    })() {
        tracing::error!("{}", e)
    }
}
// #Unsupported operating systems
