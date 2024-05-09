fn main() {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/buffer.rs")
        .input_extern_file("src/error.rs")
        .csharp_dll_name("unio")
        // .generate_csharp_file("../Unio.Unity/Assets/Unio/Generated/NativeMethods.cs")
        .generate_csharp_file("../Unio.Unity/Assets/Unio/NativeMethods.g.cs")
        .unwrap();
}