using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public struct ShaderGraphRequirements
    {
        public NeededCoordinateSpace requiresNormal;
        public NeededCoordinateSpace requiresBitangent;
        public NeededCoordinateSpace requiresTangent;
        public NeededCoordinateSpace requiresViewDir;
        public NeededCoordinateSpace requiresPosition;
        public bool requiresScreenPosition;
        public bool requiresVertexColor;
        public bool requiresFaceSign;
        public List<UVChannel> requiresMeshUVs;
        public bool requiresDepthTexture;
        public bool requiresCameraOpaqueTexture;

        public static ShaderGraphRequirements none
        {
            get
            {
                return new ShaderGraphRequirements
                {
                    requiresMeshUVs = new List<UVChannel>()
                };
            }
        }

        public bool NeedsTangentSpace()
        {
            var compoundSpaces = requiresBitangent | requiresNormal | requiresPosition
                | requiresTangent | requiresViewDir | requiresPosition
                | requiresNormal;

            return (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
        }

        public ShaderGraphRequirements Union(ShaderGraphRequirements other)
        {
            var newReqs = new ShaderGraphRequirements();
            newReqs.requiresNormal = other.requiresNormal | requiresNormal;
            newReqs.requiresTangent = other.requiresTangent | requiresTangent;
            newReqs.requiresBitangent = other.requiresBitangent | requiresBitangent;
            newReqs.requiresViewDir = other.requiresViewDir | requiresViewDir;
            newReqs.requiresPosition = other.requiresPosition | requiresPosition;
            newReqs.requiresScreenPosition = other.requiresScreenPosition | requiresScreenPosition;
            newReqs.requiresVertexColor = other.requiresVertexColor | requiresVertexColor;
            newReqs.requiresFaceSign = other.requiresFaceSign | requiresFaceSign;
            newReqs.requiresDepthTexture = other.requiresDepthTexture | requiresDepthTexture;
            newReqs.requiresCameraOpaqueTexture = other.requiresCameraOpaqueTexture | requiresCameraOpaqueTexture;

            newReqs.requiresMeshUVs = new List<UVChannel>();
            if (requiresMeshUVs != null)
                newReqs.requiresMeshUVs.AddRange(requiresMeshUVs);
            if (other.requiresMeshUVs != null)
                newReqs.requiresMeshUVs.AddRange(other.requiresMeshUVs);
            return newReqs;
        }

        public static ShaderGraphRequirements FromNodes<T>(List<T> nodes, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool includeIntermediateSpaces = true)
            where T : class, INode
        {
            NeededCoordinateSpace requiresNormal = nodes.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));
            NeededCoordinateSpace requiresBitangent = nodes.OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresBitangent(stageCapability));
            NeededCoordinateSpace requiresTangent = nodes.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability));
            NeededCoordinateSpace requiresViewDir = nodes.OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresViewDirection(stageCapability));
            NeededCoordinateSpace requiresPosition = nodes.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));
            bool requiresScreenPosition = nodes.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
            bool requiresVertexColor = nodes.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());
            bool requiresFaceSign = nodes.OfType<IMayRequireFaceSign>().Any(x => x.RequiresFaceSign());
            bool requiresDepthTexture = nodes.OfType<IMayRequireDepthTexture>().Any(x => x.RequiresDepthTexture());
            bool requiresCameraOpaqueTexture = nodes.OfType<IMayRequireCameraOpaqueTexture>().Any(x => x.RequiresCameraOpaqueTexture());

            var meshUV = new List<UVChannel>();
            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (nodes.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                    meshUV.Add(channel);
            }

            // if anything needs tangentspace we have make
            // sure to have our othonormal basis!
            if (includeIntermediateSpaces)
            {
                var compoundSpaces = requiresBitangent | requiresNormal | requiresPosition
                    | requiresTangent | requiresViewDir | requiresPosition
                    | requiresNormal;

                var needsTangentSpace = (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
                if (needsTangentSpace)
                {
                    requiresBitangent |= NeededCoordinateSpace.World;
                    requiresNormal |= NeededCoordinateSpace.World;
                    requiresTangent |= NeededCoordinateSpace.World;
                }
            }

            var reqs = new ShaderGraphRequirements()
            {
                requiresNormal = requiresNormal,
                requiresBitangent = requiresBitangent,
                requiresTangent = requiresTangent,
                requiresViewDir = requiresViewDir,
                requiresPosition = requiresPosition,
                requiresScreenPosition = requiresScreenPosition,
                requiresVertexColor = requiresVertexColor,
                requiresFaceSign = requiresFaceSign,
                requiresMeshUVs = meshUV,
                requiresDepthTexture = requiresDepthTexture,
                requiresCameraOpaqueTexture = requiresCameraOpaqueTexture
            };

            return reqs;
        }
    }
}
