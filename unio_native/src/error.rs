use std::io;
use std::string::FromUtf16Error;

#[repr(C)]
#[derive(Debug)]
pub struct UnioError {
    message: String,
}


impl From<io::Error> for UnioError {
    fn from(value: io::Error) -> Self {
        UnioError {
            message: value.to_string(),
        }
    }
}

impl From<FromUtf16Error> for UnioError {
    fn from(value: FromUtf16Error) -> Self {
        UnioError {
            message: value.to_string(),
        }
    }
}

impl From<&str> for UnioError {
    fn from(value: &str) -> Self {
        UnioError {
            message: value.to_string(),
        }
    }
}

