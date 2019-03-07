require_relative "base_generator.rb"

class GetRequestGenerator < BaseGenerator
  def initialize(basePath, path, data)
    super()
    @basePath = basePath
    @path = path
    @data = data
  end

  def write_request_comments(request)
    write_docs_summary request
    request["parameters"].each do |parameter|
      write_docs_param parameter
    end
  end

  def resolve_method_parameter(parameter)
    parameter_type = resolve_type(parameter)
    if parameter["required"]
      "#{parameter_type} #{lower_camel_case(parameter["name"])}"
    else
      parameter_type += "?" unless parameter_type == "string"
      "#{parameter_type} #{lower_camel_case(parameter["name"])} = null"
    end
  end

  def resolve_method_parameters(request)
    request["parameters"].map {|parameter| resolve_method_parameter(parameter)}.join(", ")
  end

  def write_method_path_parameter_processing(parameter)
    write "path = path.Replace(\"{#{parameter["name"]}}\", #{lower_camel_case(parameter["name"])}.ToString());"
  end

  def write_method_query_parameter_processing(parameter)
    if parameter["required"]
      write "queryList.Add(\"#{parameter["name"]}=\"+#{lower_camel_case(parameter["name"])});"
    else
      write_block "if (#{lower_camel_case(parameter["name"])} != null)" do
        write "queryList.Add(\"#{parameter["name"]}=\"+#{lower_camel_case(parameter["name"])});"
      end
    end
  end

  def write_method_parameters_processing(request)
    request["parameters"].each do |parameter|
      case parameter["in"]
      when "path"
        write_method_path_parameter_processing parameter
      when "query"
        write_method_query_parameter_processing parameter
      end
    end
  end

  def write_method_body(request, response)
    use_query = request["parameters"].any? {|parameter| parameter["in"] == "query"}
    write "string path = \"#{@basePath}#{@path}\";"
    write "List<string> queryList = new List<string>();" if use_query
    write_method_parameters_processing request
    write "string query = string.Join(\"&\", queryList.ToArray());" if use_query
    write "string query = string.Empty;" unless use_query
    write "var response = GetResponse(path, query);"
    write "return ParseResponse<#{resolve_type(response["schema"])}>(response);"
  end

  def write_method(request, response)
    deprecated = request["deprecated"]
    type = resolve_type(response["schema"])
    name = upper_camel_case(request["summary"])
    name.gsub!("Gets", "Get")
    parameters = resolve_method_parameters(request)

    write "[System.Obsolete]" if deprecated
    write_block "public #{type} #{name}(#{parameters})" do
      write_method_body(request, response)
    end
  end

  def generate
    super()

    begin
      request = @data["get"]
      return if request.nil?

      response = request["responses"]["200"]
      return if response.nil?

      write_request_comments request
      write_method(request, response)
    rescue
      return
    end
    
    @output
  end
end