use sysinfo::Pid;
use winapi::{
    shared::{
        minwindef::{BOOL, DWORD, LPARAM},
        windef::HWND,
    },
    um::{
        processthreadsapi::GetCurrentProcessId,
        winuser::{EnumWindows, GetWindowThreadProcessId, IsHungAppWindow},
    }
};
use std::{
     io, sync::{
     atomic::{
        AtomicBool,
        Ordering,
     }, Arc
    }, thread, time
};

#[cfg(windows)]
pub fn resume(target_process_id: Pid) -> bool {
    unsafe {
        let sys = sysinfo::System::new_all();
        let _proc = sys.process(target_process_id);
        if let Some(_proc) = _proc {
            kernel32::DebugActiveProcessStop(target_process_id.as_u32());
        }
    }
    return true;
}

#[cfg(not(windows))]
pub fn resume(target_process_id: Pid) -> bool {
    todo!()
}

#[cfg(windows)]
pub fn suspend(target_process_id: Pid) -> bool {
    unsafe {
        let sys = sysinfo::System::new_all();
        let _proc = sys.process(target_process_id);
        if let Some(_proc) = _proc {
            kernel32::DebugActiveProcess(target_process_id.as_u32());
        }
    }
    return true;
}

#[cfg(not(windows))]
pub fn suspend(target_process_id: Pid) -> bool {
    todo!()
}

pub fn spawn_thread_is_process_dead(
    target_process_id: Pid,
    should_check: Arc<AtomicBool>,
    out_true_when_process_is_dead: Arc<AtomicBool>,
) {
    thread::spawn(move || -> io::Result<()> {
        let mut sys = sysinfo::System::new_all();
        loop {
            if !sys.refresh_process(target_process_id) && should_check.load(Ordering::Relaxed) {
                break;
            }

            thread::sleep(time::Duration::from_millis(2000));
        }

        tracing::info!("Target process is dead, setting out_true_when_process_is_dead");
        out_true_when_process_is_dead.store(true, Ordering::Relaxed);

        Ok(())
    });
}

pub fn kill(target_process_id: Pid, callback: impl FnOnce()) {
    let sys = sysinfo::System::new_all();
    if let Some(proc) = sys.process(target_process_id) {
        proc.kill();
        callback();
    }
}

#[cfg(windows)]
pub fn spawn_thread_check_if_process_is_hung(callback: impl Fn() + std::marker::Send + 'static) {
    thread::spawn(move || -> io::Result<()> {
        unsafe {
            static mut CURRENT_PROCESS_ID: u32 = 0;
            CURRENT_PROCESS_ID = GetCurrentProcessId();

            static mut GOT_RESULT: bool = false;

            static mut WINDOW_HANDLE: HWND = 0 as _;

            GOT_RESULT = false;
            extern "system" fn enum_window(window: HWND, _: LPARAM) -> BOOL {
                unsafe {
                    if GOT_RESULT {
                        return true.into();
                    }

                    let mut proc_id: DWORD = 0 as DWORD;
                    let _ = GetWindowThreadProcessId(window, std::ptr::addr_of_mut!(proc_id));
                    if proc_id == CURRENT_PROCESS_ID {
                        WINDOW_HANDLE = window;
                    }

                    true.into()
                }
            }

            EnumWindows(Some(enum_window), 0 as LPARAM);

            let mut i = 0;
            loop {
                if IsHungAppWindow(WINDOW_HANDLE) == 1 {
                    if i == 3 {
                        callback();
                        tracing::info!("called callback!");
                        return Ok(());
                    }

                    i += 1;
                }

                thread::sleep(time::Duration::from_millis(1000));
            }
        }
    });
}

#[cfg(not(windows))]
pub(crate) fn spawn_thread_check_if_process_is_hung(
    target_process_id: Pid,
    should_check: Arc<AtomicBool>,
    out_true_when_process_is_dead: Arc<AtomicBool>,
) {
    todo!()
}
