Shader "Unlit/StencilShader"
{
    Properties
    {
        _StencilRef("Stencil Ref", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" } // 提前渲染
        Pass
        {
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
            ColorMask 0 // 不畫顏色，只寫模板
        }
    }
}
