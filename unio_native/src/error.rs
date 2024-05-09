use std::io;
use std::string::FromUtf16Error;
use crate::buffer::CSharpString;

#[repr(C)]
#[derive(Debug)]
pub struct UnioError {
    message: CSharpString,
}

impl From<io::Error> for UnioError {
    fn from(value: io::Error) -> Self {
        UnioError {
            message: value.to_string().into(),
        }
    }
}

impl From<FromUtf16Error> for UnioError {
    fn from(value: FromUtf16Error) -> Self {
        UnioError {
            message: value.to_string().into(),
        }
    }
}

impl From<&str> for UnioError {
    fn from(value: &str) -> Self {
        UnioError {
            message: value.to_string().into(),
        }
    }
}

