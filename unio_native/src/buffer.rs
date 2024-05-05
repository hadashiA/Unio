use crate::error::UnioError;

pub struct CSharpString {
    ptr: *const u16,
    length: i32,
}

impl CSharpString {
    pub fn new(ptr: *const u16, length: i32) -> Self {
        Self { ptr, length }
    }

    pub unsafe fn to_string(&self) -> Result<String, UnioError> {
        let slice = std::slice::from_raw_parts(self.ptr, self.length as usize);
        Ok(String::from_utf16(slice)?)
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
            .map_err(|err| UnioError::from("buffer length negative or overflowed"))
    }

    pub fn from_vec(bytes: Vec<u8>) -> Result<Self, UnioError> {
        let length = i32::try_from(bytes.len())
            .map_err(|err| UnioError::from("buffer length cannot fit into a i32."))?;

        let capacity = i32::try_from(bytes.capacity())
            .map_err(|err| UnioError::from("buffer capacity cannot fit into a i32."))?;

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
