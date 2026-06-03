fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_prost_build::configure()
        .build_client(true)
        .build_server(true)
        .compile_protos(
            &["../proto/comms.proto"],
            &["../proto/"],
        )?;
    println!("cargo:rerun-if-changed=../proto/comms.proto");
    Ok(())
}
