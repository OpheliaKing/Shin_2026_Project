using UnityEngine;

namespace Shin
{
    public partial class PlayerCharacterBase
    {
        protected override void ZoomStateChange()
        {
            base.ZoomStateChange();
            ActiveCamera(PLAYER_CAMERA_TYPE.SHOOT_ZOOM, IsZoomState);
            SetWeight("UpperBody", IsZoomState ? 1f : 0f);
        }
    }
}
