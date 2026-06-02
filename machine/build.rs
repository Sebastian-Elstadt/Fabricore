fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_prost_build::configure()
        .build_client(true)
        .build_server(true)
        .compile_protos(
            &["src/comms/comms.proto"],
            &["src/comms"],
        )?;
    println!("cargo:rerun-if-changed=src/comms/comms.proto");
    Ok(())
}
