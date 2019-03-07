require_relative "base_generator.rb"
require_relative "get_request_generator.rb"

class RequestsGenerator < BaseGenerator
  def initialize(name, data)
    super()
    @data = data
    @name = name
  end

  def write_methods
    @data["paths"].each do |path, data|
      generator = GetRequestGenerator.new(@data["basePath"], path, data)
      write generator.generate
    end
  end

  def generate
    super()

    write "using PatchKit.Api.Models.#{@name};"
    write "using System.Collections.Generic;"
    write nil
    write_block "namespace PatchKit.Api" do
      write_block "public partial class #{@name}ApiConnection" do
        write_methods
      end
    end

    @output
  end
end
