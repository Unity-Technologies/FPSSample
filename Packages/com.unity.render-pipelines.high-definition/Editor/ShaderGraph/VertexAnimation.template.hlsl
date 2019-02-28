
VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

    $VertexDescriptionInputs.ObjectSpaceNormal:         output.ObjectSpaceNormal =           input.normalOS;
    $VertexDescriptionInputs.WorldSpaceNormal:          output.WorldSpaceNormal =            TransformObjectToWorldNormal(input.normalOS);
    $VertexDescriptionInputs.ViewSpaceNormal:           output.ViewSpaceNormal =             TransformWorldToViewDir(output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceNormal:        output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);
    $VertexDescriptionInputs.ObjectSpaceTangent:        output.ObjectSpaceTangent =          input.tangentOS;
    $VertexDescriptionInputs.WorldSpaceTangent:		    output.WorldSpaceTangent =           TransformObjectToWorldDir(input.tangentOS.xyz);
    $VertexDescriptionInputs.ViewSpaceTangent:          output.ViewSpaceTangent =            TransformWorldToViewDir(output.WorldSpaceTangent);
    $VertexDescriptionInputs.TangentSpaceTangent:       output.TangentSpaceTangent =         float3(1.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpaceBiTangent:      output.ObjectSpaceBiTangent =        normalize(cross(input.normalOS, input.tangentOS) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
    $VertexDescriptionInputs.WorldSpaceBiTangent:       output.WorldSpaceBiTangent =         TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
    $VertexDescriptionInputs.ViewSpaceBiTangent:        output.ViewSpaceBiTangent =          TransformWorldToViewDir(output.WorldSpaceBiTangent);
    $VertexDescriptionInputs.TangentSpaceBiTangent:     output.TangentSpaceBiTangent =       float3(0.0f, 1.0f, 0.0f);
    $VertexDescriptionInputs.ObjectSpacePosition:       output.ObjectSpacePosition =         input.positionOS;
    $VertexDescriptionInputs.WorldSpacePosition:        output.WorldSpacePosition =          GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
    $VertexDescriptionInputs.ViewSpacePosition:         output.ViewSpacePosition =           TransformWorldToView(output.WorldSpacePosition);
    $VertexDescriptionInputs.TangentSpacePosition:      output.TangentSpacePosition =        float3(0.0f, 0.0f, 0.0f);
    $VertexDescriptionInputs.WorldSpaceViewDirection:   output.WorldSpaceViewDirection =     GetWorldSpaceNormalizeViewDir(output.WorldSpacePosition);
    $VertexDescriptionInputs.ObjectSpaceViewDirection:  output.ObjectSpaceViewDirection =    TransformWorldToObjectDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.ViewSpaceViewDirection:    output.ViewSpaceViewDirection =      TransformWorldToViewDir(output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.TangentSpaceViewDirection: float3x3 tangentSpaceTransform =     float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
    $VertexDescriptionInputs.TangentSpaceViewDirection: output.TangentSpaceViewDirection =   mul(tangentSpaceTransform, output.WorldSpaceViewDirection);
    $VertexDescriptionInputs.ScreenPosition:            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(output.WorldSpacePosition), _ProjectionParams.x);
    $VertexDescriptionInputs.uv0:                       output.uv0 =                         input.uv0;
    $VertexDescriptionInputs.uv1:                       output.uv1 =                         input.uv1;
    $VertexDescriptionInputs.uv2:                       output.uv2 =                         input.uv2;
    $VertexDescriptionInputs.uv3:                       output.uv3 =                         input.uv3;
    $VertexDescriptionInputs.VertexColor:               output.VertexColor =                 input.color;

    return output;
}

AttributesMesh ApplyMeshModification(AttributesMesh input)
{
    // build graph inputs
    VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);

    // evaluate vertex graph
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);

    // copy graph output to the results
    $VertexDescription.Position:    input.positionOS = vertexDescription.Position;

    return input;
}
