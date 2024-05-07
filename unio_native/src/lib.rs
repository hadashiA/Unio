use std::fs::File;
use std::io::Read;

use crate::{
    buffer::{ByteBuffer, CSharpString},
    error::UnioError
};

mod buffer;
mod error;

#[repr(C)]
pub struct ReadResult {
    bytes: ByteBuffer,
    error: *mut UnioError,
}

#[no_mangle]
pub extern "C" fn unio_read_result_delete(value: ReadResult) {
    if !value.error.is_null() {
        unio_boxed_error_delete(value.error);
    }
    unio_byte_buffer_delete(value.bytes);
}

#[no_mangle]
pub extern "C" fn unio_byte_buffer_delete(bytes: ByteBuffer) {
    drop(bytes.into_vec())
}

#[no_mangle]
pub extern "C" fn unio_boxed_error_delete(error: *mut UnioError) {
    unsafe { let _ = Box::from_raw(error); };
}

#[no_mangle]
pub unsafe extern "C" fn unio_file_read_to_end(
    path: *const u16,
    path_length: i32,
) -> ReadResult {

    let result = read_file_by_utf16_path(path, path_length);
    match result {
        Ok(bytes) => ReadResult {
            error: std::ptr::null_mut(),
            bytes,
        },
        Err(error) => ReadResult {
            error: Box::into_raw(Box::new(error)),
            bytes: ByteBuffer::default(),
        }
    }
}

fn read_file_by_utf16_path(path: *const u16, path_length: i32) -> Result<ByteBuffer, UnioError> {
    let path_length: usize = path_length
        .try_into()
        .map_err(|_| "path length negative or overflowed")?;

    let path = CSharpString::new(path, path_length);
    let path = path.to_string()?;

    let mut file = File::open(path)?;
    let file_size = file.metadata()?.len();
    let mut buffer = vec![0; file_size as usize];
    file.read_to_end(&mut buffer)?;
    return ByteBuffer::from_vec(buffer);
}