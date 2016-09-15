using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class UnityEnginePredefinedType
    {
		[NotNull] public static readonly IClrTypeName AUDIO_CLIP_FQN = new ClrTypeName("UnityEngine.AudioClip");
        [NotNull] public static readonly IClrTypeName BIT_STREAM_FQN = new ClrTypeName("UnityEngine.BitStream");
        [NotNull] public static readonly IClrTypeName COLLIDER_FQN = new ClrTypeName("UnityEngine.Collider");
        [NotNull] public static readonly IClrTypeName COLLIDER_2D_FQN = new ClrTypeName("UnityEngine.Collider2D");
        [NotNull] public static readonly IClrTypeName COLLISION_FQN = new ClrTypeName("UnityEngine.Collision");
        [NotNull] public static readonly IClrTypeName COLLISION_2D_FQN = new ClrTypeName("UnityEngine.Collision2D");
        [NotNull] public static readonly IClrTypeName CONTROLLER_COLLIDER_HIT_FQN = new ClrTypeName("UnityEngine.ControllerColliderHit");
        [NotNull] public static readonly IClrTypeName GAME_OBJECT_FQN = new ClrTypeName("UnityEngine.GameObject");
        [NotNull] public static readonly IClrTypeName MASTER_SERVER_EVENT_FQN = new ClrTypeName("UnityEngine.MasterServerEvent");
		[NotNull] public static readonly IClrTypeName MATERIAL_FQN = new ClrTypeName("UnityEngine.Material");
        [NotNull] public static readonly IClrTypeName NETWORK_CONNECTION_ERROR_FQN = new ClrTypeName("UnityEngine.NetworkConnectionError");
        [NotNull] public static readonly IClrTypeName NETWORK_DISCONNECTION_FQN = new ClrTypeName("UnityEngine.NetworkDisconnection");
        [NotNull] public static readonly IClrTypeName NETWORK_MESSAGE_INFO_FQN = new ClrTypeName("UnityEngine.NetworkMessageInfo");
        [NotNull] public static readonly IClrTypeName NETWORK_PLAYER_FQN = new ClrTypeName("UnityEngine.NetworkPlayer");
        [NotNull] public static readonly IClrTypeName RENDER_TEXTURE_FQN = new ClrTypeName("UnityEngine.RenderTexture");
		[NotNull] public static readonly IClrTypeName RENDERER_FQN = new ClrTypeName("UnityEngine.Renderer");
		[NotNull] public static readonly IClrTypeName SPRITE_FQN = new ClrTypeName("UnityEngine.Sprite");
		[NotNull] public static readonly IClrTypeName TEXTURE_2D_FQN = new ClrTypeName("UnityEngine.Texture2D");
	}
}