using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Example;

[GrpcMessage]
public partial class SimpleMessage
{
    public string Id { get; set; }
    public string? Username { get; set; }
}

[GrpcService]
public partial class SimpleService
{

}
