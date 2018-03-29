// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Stereo 3D View Methods" {
Properties {
   _LeftTex ("Left (RGB)", RECT) = "white" {}
   _RightTex ("Right (RGB)", RECT) = "white" {}
   _Balance_Left_R 		("Balance Left R", Vector) = (0, 0, 0, 0)
   _Balance_Left_G 		("Balance Left G", Vector) = (0, 0, 0, 0)
   _Balance_Left_B 		("Balance Left B", Vector) = (0, 0, 0, 0)
   
   _Balance_Right_R 	("Balance Right R", Vector) = (0, 0, 0, 0)
   _Balance_Right_G		("Balance Right G", Vector) = (0, 0, 0, 0)
   _Balance_Right_B		("Balance Right B", Vector) = (0, 0, 0, 0)
   
   _Weave_X ("Weave X", Float) = 1920
   _Weave_Y ("Weave Y", Float) = 1080
}

SubShader {
	
	// **** PASS 0 ANAGLYPH ****
    Pass {
      ZTest Always Cull Off ZWrite Off
      Fog { Mode off }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma fragmentoption ARB_precision_hint_fastest
      #include "UnityCG.cginc"
      
      uniform sampler2D _LeftTex;
      uniform sampler2D _RightTex;
      uniform float4 _Balance_Left_R;
      uniform float4 _Balance_Left_G;
      uniform float4 _Balance_Left_B;
      uniform float4 _Balance_Right_R;
      uniform float4 _Balance_Right_G;
      uniform float4 _Balance_Right_B;
      
      struct v2f {
         float4 pos : POSITION;
         float2 uv : TEXCOORD0;
      };
      v2f vert( appdata_img v ) {
         v2f o;
         o.pos = UnityObjectToClipPos (v.vertex);
         float2 uv = MultiplyUV( UNITY_MATRIX_TEXTURE0, v.texcoord );
         o.uv = uv;
         return o;
      }
      half4 frag (v2f i) : COLOR {
         float ra, ga, ba, r1, g1, b1, r2, g2, b2;
         float4 texL = tex2D(_LeftTex, i.uv);
         float4 texR = tex2D(_RightTex, i.uv);
         float4 texRGB;
         
         r1	=	texL.r * _Balance_Left_R[0] + texL.g * _Balance_Left_R[1] + texL.b * _Balance_Left_R[2];
         g1	=	texL.r * _Balance_Left_G[0] + texL.g * _Balance_Left_G[1] + texL.b * _Balance_Left_G[2];
         b1	=	texL.r * _Balance_Left_B[0] + texL.g * _Balance_Left_B[1] + texL.b * _Balance_Left_B[2];
         r2	=	texR.r * _Balance_Right_R[0] + texR.g * _Balance_Right_R[1] + texR.b * _Balance_Right_R[2];
         g2	=	texR.r * _Balance_Right_G[0] + texR.g * _Balance_Right_G[1] + texR.b * _Balance_Right_G[2];
         b2	=	texR.r * _Balance_Right_B[0] + texR.g * _Balance_Right_B[1] + texR.b * _Balance_Right_B[2];
        
         texRGB = float4(r1+r2,g1+g2,b1+b2,1);
         return texRGB;
      }
      ENDCG
   }
   
	// **** PASS 1 LEFT ****
   Pass {
      ZTest Always Cull Off ZWrite Off
      Fog { Mode off }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma fragmentoption ARB_precision_hint_fastest
      #include "UnityCG.cginc"
      
      uniform sampler2D _LeftTex;
      uniform float4 _Balance_Left_R;
      uniform float4 _Balance_Left_G;
      uniform float4 _Balance_Left_B;
      
      struct v2f {
         float4 pos : POSITION;
         float2 uv : TEXCOORD0;
      };
      v2f vert( appdata_img v ) {
         v2f o;
         o.pos = UnityObjectToClipPos (v.vertex);
         float2 uv = MultiplyUV( UNITY_MATRIX_TEXTURE0, v.texcoord );
         o.uv = uv;
         return o;
      }
      half4 frag (v2f i) : COLOR {
         float ra, ga, ba, r1, g1, b1;
         float4 texL = tex2D(_LeftTex, i.uv);
         float4 texRGB;
       	 r1	=	texL.r;
         g1	=	texL.g;
         b1	=	texL.b;
         texRGB = float4(r1,g1,b1,1);
         return texRGB;
      }
      ENDCG
   }
   
   // **** PASS 2 RIGHT ****
   Pass {
      ZTest Always Cull Off ZWrite Off
      Fog { Mode off }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma fragmentoption ARB_precision_hint_fastest
      #include "UnityCG.cginc"
      
      uniform sampler2D _RightTex;
      uniform float4 _Balance_Right_R;
      uniform float4 _Balance_Right_G;
      uniform float4 _Balance_Right_B;
      
      struct v2f {
         float4 pos : POSITION;
         float2 uv : TEXCOORD0;
      };
      v2f vert( appdata_img v ){
         v2f o;
         o.pos = UnityObjectToClipPos (v.vertex);
         float2 uv = MultiplyUV( UNITY_MATRIX_TEXTURE0, v.texcoord );
         o.uv = uv;
         return o;
      }
      half4 frag (v2f i) : COLOR{
         float ra, ga, ba, r2, g2, b2;
         float4 texR = tex2D(_RightTex, i.uv);
         float4 texRGB;
         r2	=	texR.r;
         g2	=	texR.g;
         b2	=	texR.b;
         texRGB = float4(r2,g2,b2,1);
         return texRGB;
      }
      ENDCG
  	}
  	
  	// **** PASS 3 INTERLACE/CHECKERBOARD ****
	Pass {
		ZTest Always
		Cull Off
		ZWrite Off
		Fog {
			Mode off
		}
		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
		#pragma fragmentoption ARB_precision_hint_fastest
		#include "UnityCG.cginc"
		
		uniform float _Weave_X;
		uniform float _Weave_Y;
		uniform sampler2D	_LeftTex;
		uniform sampler2D	_RightTex;

		float4 frag( v2f_img IN ) : COLOR0 {
			float3 left = tex2D( _LeftTex, IN.uv ).rgb;		// Sample scene texture
			float3 right = tex2D( _RightTex, IN.uv ).rgb;	// Sample scene texture
			
			int x = fmod(floor( IN.uv.x*_Weave_X) + floor(IN.uv.y*_Weave_Y),2) < 1;					
			return float4( lerp( left, right, x ), 0.0 );
		}
		ENDCG
	}
}   
   Fallback off
} 
