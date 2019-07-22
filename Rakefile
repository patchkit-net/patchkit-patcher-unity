libpkapps_output_dir = "../libpkapps/PatchKit.LibBridge/bin/Release/netstandard2.0"
libpkapps_output_files = Rake::FileList[
  "#{libpkapps_output_dir}/*.dll",
  "#{libpkapps_output_dir}/*.so",
  "#{libpkapps_output_dir}/*.bundle" ]

task "update-libpkapps" do |t|
  sh "cd #{libpkapps_output_dir} && rake build-dotnet"
  libpkapps_output_files.each do |f|
    cp(f,
      "Assets/PatchKit Patcher/Library/") unless f.include?("JetBrains")
  end
end