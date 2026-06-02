using UnityEngine;

namespace Shin
{
    public class GameManager : SingtonObject<GameManager>
    {
        private CombatManager _combatManager;

        public CombatManager CombatManager =>
            ManagerBase.GetOrCreate(transform, ref _combatManager);

        override protected void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            //Cursor.visible = false;
        }
    }

}
