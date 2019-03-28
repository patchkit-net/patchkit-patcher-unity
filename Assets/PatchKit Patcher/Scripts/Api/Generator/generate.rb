require 'json'
require 'fileutils'
require_relative 'model_generator.rb'
require_relative 'requests_generator.rb'

def generate(name)
  data = JSON.parse(File.read("swagger-#{name.downcase}.json"))

  data["definitions"].each do |model_name, model|
    generator = ModelGenerator.new(name, model_name, model)
    FileUtils::mkdir_p("Models/#{name}")
    File.open("Models/#{name}/#{model_name}.cs", "w") do |output|
      output.write generator.generate
    end
  end

  File.open("#{name}ApiConnection.Generated.cs", "w") do |output|
    generator = RequestsGenerator.new(name, data)
    output.write generator.generate
  end
end

generate "Main"
generate "Keys"
