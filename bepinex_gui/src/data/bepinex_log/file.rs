use crate::app;
use crate::backend::file_explorer_utils;
use std::{
    fs::{self, File},
    io::{BufReader, BufWriter},
    path::{Path, PathBuf},
};
use zip::write::FileOptions;

pub fn open_file_in_file_explorer(file_full_path: &PathBuf, zip_file_name: &str) {
    if let Ok(file_metadata) = fs::metadata(file_full_path) {
        let file_size_bytes = file_metadata.len();
        const ONE_MEGABYTE: u64 = 1_000_000;
        // check log file size, if its more than size limit, just zip it
        if file_size_bytes >= ONE_MEGABYTE {
            let zip_file_full_path = file_full_path.parent().unwrap().join(zip_file_name);
            match zip_file(&zip_file_full_path, file_full_path) {
                Ok(_) => {
                    file_explorer_utils::highlight_path_in_file_explorer(&zip_file_full_path);
                }
                Err(e) => {
                    tracing::error!("Failed zipping: {}", e.to_string());
                }
            }
        } else {
            file_explorer_utils::highlight_path_in_file_explorer(file_full_path);
        }
    }
}

pub fn zip_file<P: AsRef<Path>, P2: AsRef<Path>>(
    output_zip_file_path: P,
    input_log_file_path: P2,
) -> zip::result::ZipResult<()> {
    let zip_file = std::fs::File::create(&output_zip_file_path).unwrap();

    let mut zip = zip::ZipWriter::new(zip_file);

    let options = FileOptions::default()
        .compression_method(zip::CompressionMethod::Deflated)
        .unix_permissions(0o755);

    zip.start_file("LogOutput.log", options)?;
    let mut log_file_buffer = BufReader::new(File::open(input_log_file_path)?);
    let mut zip_buf_writer = BufWriter::new(zip);
    std::io::copy(&mut log_file_buffer, &mut zip_buf_writer)?;
    Ok(())
}

pub fn get_full_path() -> Option<PathBuf> {
    if let Some(directory_full_path) = get_app_config_directory() {
        if std::fs::create_dir_all(&directory_full_path).is_ok() {
            return Some(directory_full_path.join("log.txt"));
        }
    }

    None
}

pub fn get_app_config_directory() -> Option<PathBuf> {
    directories_next::ProjectDirs::from("", "", app::NAME)
        .map(|proj_dirs| proj_dirs.data_dir().to_path_buf())
}
