using UnityEngine;

namespace SYArtToolkit
{
    public abstract class ToolInterface
    {
        public abstract void InterfaceShow(bool atThisWindow, Rect position);

        //public virtual void OnEnable() {}
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Disable() { }
    }


    public class WithOutTool : ToolInterface
    {
        public override void InterfaceShow(bool atThisWindow, Rect positionas)
        {

        }
    }
}