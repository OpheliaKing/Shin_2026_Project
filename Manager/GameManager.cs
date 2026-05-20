using UnityEngine;

namespace Shin
{
    public class GameManager : SingtonObject<GameManager>
    {
        override protected void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            //Cursor.visible = false;
        }
    }

}
