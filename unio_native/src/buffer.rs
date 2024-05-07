use crate::error::UnioError;

#[repr(C)]
#[derive(Debug)]
pub struct CSharpString {
    ptr: *const u16,
    length: usize,
}

impl CSharpString {
    pub fn new(ptr: *const u16, length: usize) -> Self {
        Self { ptr, length }
    }

    pub fn to_string(&self) -> Result<String, UnioError> {
        let slice = unsafe {
            std::slice::from_raw_parts(self.ptr, self.length as usize)
        };
        Ok(String::from_utf16(slice)?)
    }
}

impl From<&str> for CSharpString {
    fn from(value: &str) -> Self {
        let value: Vec<u16> = value.encode_utf16().collect();
        Self {
            ptr: value.as_ptr(),
            length: value.len()
        }
    }
}

impl From<String> for CSharpString {
    fn from(value: String) -> Self {
        value.as_str().into()
    }
}

#[repr(C)]
pub struct ByteBuffer {
    ptr: *mut u8,
    length: i32,
    capacity: i32,
}

impl Default for ByteBuffer {
    fn default() -> Self {
        Self {
            ptr: std::ptr::null_mut(),
            length: 0,
            capacity: 0,
        }
    }
}

impl ByteBuffer {
    pub fn len(&self) -> Result<usize, UnioError> {
        self.length
            .try_into()
            .map_err(|_| UnioError::from("buffer length negative or overflowed"))
    }

    pub fn from_vec(bytes: Vec<u8>) -> Result<Self, UnioError> {
        let length = i32::try_from(bytes.len())
            .map_err(|_| "buffer length cannot fit into a i32.")?;

        let capacity = i32::try_from(bytes.capacity())
            .map_err(|_| "buffer capacity cannot fit into a i32.")?;

        // keep memory until call delete
        let mut v = std::mem::ManuallyDrop::new(bytes);

        Ok(Self {
            ptr: v.as_mut_ptr(),
            length,
            capacity,
        })
    }

    pub fn into_vec(self) -> Vec<u8> {
        if self.ptr.is_null() {
            vec![]
        } else {
            let capacity: usize = self
                .capacity
                .try_into()
                .expect("buffer capacity negative or overflowed");
            let length: usize = self
                .length
                .try_into()
                .expect("buffer length negative or overflowed");

            unsafe { Vec::from_raw_parts(self.ptr, length, capacity) }
        }
    }

    pub fn destroy(self) {
        drop(self.into_vec());
    }
}
