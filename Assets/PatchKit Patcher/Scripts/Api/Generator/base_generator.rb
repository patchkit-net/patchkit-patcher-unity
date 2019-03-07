require 'json'

class BaseGenerator
  def initialize
    @indent_level = 0
    @output = ""
  end

  def camel_case(text)
    text.gsub(/[\s,_](.)/) {|e| $1.upcase}
  end

  def upper_camel_case(text)
    output = camel_case(text)
    output[0, 1].upcase + output[1..-1]
  end

  def lower_camel_case(text)
    output = camel_case(text)
    output[0, 1].downcase + output[1..-1]
  end

  def resolve_int_type(type)
    case type["format"]
    when "int64"
      "long"
    else
      "int"
    end
  end

  def resolve_array_type(type)
    items_type = resolve_type(type["items"])
    "#{items_type}[]"
  end

  def resolve_base_type(type)
    case type["type"]
    when "string"
      "string"
    when "boolean"
      "bool"
    when "integer"
      resolve_int_type(type)
    when "array"
      resolve_array_type(type)
    when "number"
      resolve_number_type(type)
    else
      return type["type"].sub!("#/definitions/", "") if type["type"].start_with?("#/definitions/")
      raise "Cannot resolve base type of #{type}"
    end
  end

  def resolve_number_type(type)
    case type['format']
    when 'float'
      'float'
    when 'double'
      'double'
    else
      raise "Cannot resolve base type of #{type}"
    end
  end

  def resolve_type(type)
    if type.key? "$ref"
      type["$ref"].gsub("#/definitions/", "")
    else
      resolve_base_type(type)
    end
  end

  def write(text)
    indent = "    " * @indent_level
    indent_text = text.gsub("\n", "\n#{indent}") unless text.nil?
    @output += "#{indent}#{indent_text}\n"
  end

  def write_block(&block)
    write "{"
    @indent_level+=1
    block.call if block_given?
    @indent_level-=1
    write "}"
  end

  def write_block(text, &block)
    write text
    write "{"
    @indent_level+=1
    block.call if block_given?
    @indent_level-=1
    write "}"
  end

  def write_comment(text)
    write "/// #{text}"
  end

  def write_docs_summary(data)
    return unless data.key? "description"
    write_comment "<summary>"
    write_comment data["description"]
    write_comment "</summary>"
  end

  def write_docs_param(parameter)
    write_comment "<param name=\"#{lower_camel_case(parameter["name"])}\">#{parameter["description"]}</param>"
  end

  def generate
    @output = ""
  end
end
