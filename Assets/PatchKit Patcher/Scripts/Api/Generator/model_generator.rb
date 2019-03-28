require_relative "base_generator.rb"

class ModelGenerator < BaseGenerator
  def initialize(api_name, name, data)
    super()
    @api_name = api_name
    @name = name
    @data = data
  end

  def write_json_property(property_name)
    write "[JsonProperty(\"#{property_name}\")]"
  end

  def write_property(property_name, property)
    write_docs_summary property
    write_json_property property_name
    write "public #{resolve_type(property)} #{upper_camel_case(property_name)};"
  end

  def write_properties
    @data["properties"].each do |property_name, property|
      write_property(property_name, property)
      write nil
    end
  end

  def generate
    super()

    return if @data["type"] != "object"

    write "using Newtonsoft.Json;"
    write nil
    write_block "namespace PatchKit.Api.Models.#{@api_name}" do
      write_block "public struct #{@name}" do
        write_properties
      end
    end

    @output
  end
end