    FragInputs BuildFragInputs(VaryingsMeshToPS input)
    {
        FragInputs output;
        ZERO_INITIALIZE(FragInputs, output);

        // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
        // TODO: this is a really poor workaround, but the variable is used in a bunch of places
        // to compute normals which are then passed on elsewhere to compute other values...
        output.worldToTangent = k_identity3x3;
        output.positionSS = input.positionCS;       // input.positionCS is SV_Position

        $FragInputs.positionRWS:        output.positionRWS = input.positionRWS;
        $FragInputs.worldToTangent:     output.worldToTangent = BuildWorldToTangent(input.tangentWS, input.normalWS);
        $FragInputs.texCoord0:          output.texCoord0 = input.texCoord0;
        $FragInputs.texCoord1:          output.texCoord1 = input.texCoord1;
        $FragInputs.texCoord2:          output.texCoord2 = input.texCoord2;
        $FragInputs.texCoord3:          output.texCoord3 = input.texCoord3;
        $FragInputs.color:              output.color = input.color;
        #if SHADER_STAGE_FRAGMENT
        $FragInputs.isFrontFace:        output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);       // TODO: SHADER_STAGE_FRAGMENT only
        #endif // SHADER_STAGE_FRAGMENT

        return output;
    }

    SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
    {
        SurfaceDescriptionInputs output;
        ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

        $SurfaceDescriptionInputs.WorldSpaceNormal:          output.WorldSpaceNormal =            normalize(input.worldToTangent[2].xyz);
        $SurfaceDescriptionInputs.ObjectSpaceNormal:         output.ObjectSpaceNormal =           mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M);           // transposed multiplication by inverse matrix to handle normal scale
        $SurfaceDescriptionInputs.ViewSpaceNormal:           output.ViewSpaceNormal =             mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_I_V);         // transposed multiplication by inverse matrix to handle normal scale
        $SurfaceDescriptionInputs.TangentSpaceNormal:        output.TangentSpaceNormal =          float3(0.0f, 0.0f, 1.0f);
        $SurfaceDescriptionInputs.WorldSpaceTangent:         output.WorldSpaceTangent =           input.worldToTangent[0].xyz;
        $SurfaceDescriptionInputs.ObjectSpaceTangent:        output.ObjectSpaceTangent =          TransformWorldToObjectDir(output.WorldSpaceTangent);
        $SurfaceDescriptionInputs.ViewSpaceTangent:          output.ViewSpaceTangent =            TransformWorldToViewDir(output.WorldSpaceTangent);
        $SurfaceDescriptionInputs.TangentSpaceTangent:       output.TangentSpaceTangent =         float3(1.0f, 0.0f, 0.0f);
        $SurfaceDescriptionInputs.WorldSpaceBiTangent:       output.WorldSpaceBiTangent =         input.worldToTangent[1].xyz;
        $SurfaceDescriptionInputs.ObjectSpaceBiTangent:      output.ObjectSpaceBiTangent =        TransformWorldToObjectDir(output.WorldSpaceBiTangent);
        $SurfaceDescriptionInputs.ViewSpaceBiTangent:        output.ViewSpaceBiTangent =          TransformWorldToViewDir(output.WorldSpaceBiTangent);
        $SurfaceDescriptionInputs.TangentSpaceBiTangent:     output.TangentSpaceBiTangent =       float3(0.0f, 1.0f, 0.0f);
        $SurfaceDescriptionInputs.WorldSpaceViewDirection:   output.WorldSpaceViewDirection =     normalize(viewWS);
        $SurfaceDescriptionInputs.ObjectSpaceViewDirection:  output.ObjectSpaceViewDirection =    TransformWorldToObjectDir(output.WorldSpaceViewDirection);
        $SurfaceDescriptionInputs.ViewSpaceViewDirection:    output.ViewSpaceViewDirection =      TransformWorldToViewDir(output.WorldSpaceViewDirection);
        $SurfaceDescriptionInputs.TangentSpaceViewDirection: float3x3 tangentSpaceTransform =     float3x3(output.WorldSpaceTangent,output.WorldSpaceBiTangent,output.WorldSpaceNormal);
        $SurfaceDescriptionInputs.TangentSpaceViewDirection: output.TangentSpaceViewDirection =   mul(tangentSpaceTransform, output.WorldSpaceViewDirection);
        $SurfaceDescriptionInputs.WorldSpacePosition:        output.WorldSpacePosition =          GetAbsolutePositionWS(input.positionRWS);
        $SurfaceDescriptionInputs.ObjectSpacePosition:       output.ObjectSpacePosition =         TransformWorldToObject(input.positionRWS);
        $SurfaceDescriptionInputs.ViewSpacePosition:         output.ViewSpacePosition =           TransformWorldToView(input.positionRWS);
        $SurfaceDescriptionInputs.TangentSpacePosition:      output.TangentSpacePosition =        float3(0.0f, 0.0f, 0.0f);
        $SurfaceDescriptionInputs.ScreenPosition:            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);
        $SurfaceDescriptionInputs.uv0:                       output.uv0 =                         input.texCoord0;
        $SurfaceDescriptionInputs.uv1:                       output.uv1 =                         input.texCoord1;
        $SurfaceDescriptionInputs.uv2:                       output.uv2 =                         input.texCoord2;
        $SurfaceDescriptionInputs.uv3:                       output.uv3 =                         input.texCoord3;
        $SurfaceDescriptionInputs.VertexColor:               output.VertexColor =                 input.color;
        $SurfaceDescriptionInputs.FaceSign:                  output.FaceSign =                    input.isFrontFace;

        return output;
    }

    // existing HDRP code uses the combined function to go directly from packed to frag inputs
    FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
    {
        VaryingsMeshToPS unpacked= UnpackVaryingsMeshToPS(input);
        return BuildFragInputs(unpacked);
    }
