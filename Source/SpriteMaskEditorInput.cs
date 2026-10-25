using UnityEngine;
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        private const int TexturePreviewControlHint=0x53505256;
        private int textureBrowserPreviewControl;
        private void CancelTexturePreviewPointer()
        {
            if(textureBrowserPreviewDragging && textureBrowserPreviewControl!=0) browserGuiReleaseHotControl=textureBrowserPreviewControl;
            textureBrowserPreviewDragging=false;textureBrowserPreviewControl=0;
        }
        private void FinishTexturePreviewPointer()
        {
            int captured=textureBrowserPreviewControl;CancelTexturePreviewPointer();
            if(GUIUtility.hotControl==captured) GUIUtility.hotControl=0;
            if(browserGuiReleaseHotControl==captured) browserGuiReleaseHotControl=0;
        }
        private void HandleTexturePreviewInput(Rect previewRect)
        {
            Event e=Event.current;if(e==null)return;
            int control=GUIUtility.GetControlID(TexturePreviewControlHint,FocusType.Passive,previewRect);
            if(textureBrowserPreviewDragging)
            {
                if(e.rawType==EventType.MouseUp && e.button==0)
                { FinishTexturePreviewPointer();if(e.type!=EventType.Used)e.Use();return; }
                if(GUIUtility.hotControl!=textureBrowserPreviewControl || (e.type==EventType.Repaint&&!Input.GetMouseButton(0)))
                { FinishTexturePreviewPointer();return; }
                if(e.type==EventType.MouseDrag&&e.button==0)
                { textureBrowserPreviewPan+=e.delta;e.Use();return; }
                if(previewRect.Contains(e.mousePosition)&&(e.type==EventType.MouseDown||e.type==EventType.ScrollWheel))e.Use();
                return;
            }
            if(!GUI.enabled||!previewRect.Contains(e.mousePosition)||GUIUtility.hotControl!=0)return;
            if(e.type==EventType.ScrollWheel)
            {
                float zoomFactor=e.delta.y>0 ? .9f : 1.1f;
                textureBrowserPreviewZoom=Mathf.Clamp(textureBrowserPreviewZoom*zoomFactor,.25f,16f);e.Use();return;
            }
            if(e.type==EventType.MouseDown&&e.button==0)
            { textureBrowserPreviewDragging=true;textureBrowserPreviewControl=control;GUIUtility.hotControl=control;e.Use(); }
        }
        private const int MaskEditorControlHint=0x534D4153;
        private bool HasMaskEditorPointer { get { return maskEditorDragging||maskEditorPanning; } }
        private bool ShowingMaskLibrary { get { return maskEditorOptions && maskLibraryMode; } }
        private bool CanUseMaskImageControls { get { return !ShowingMaskLibrary; } }
        private bool CanApplyMaskSelection { get { return CanUseMaskImageControls && SpriteMaskRectangle.HasArea(maskEditorPoints)&&!HasMaskEditorPointer; } }
        private void CancelMaskEditorPointer()
        {
            if(HasMaskEditorPointer && maskEditorControl!=0) browserGuiReleaseHotControl=maskEditorControl;
            maskEditorDragging=false;maskEditorPanning=false;maskEditorControl=0;
        }
        private void ReleasePendingMaskEditorPointer()
        {
            if(browserGuiReleaseHotControl==0) return;
            if(GUIUtility.hotControl==browserGuiReleaseHotControl) GUIUtility.hotControl=0;
            browserGuiReleaseHotControl=0;
        }
        private void CancelMaskPointerOnFocusLoss()
        {
            if(!Application.isFocused) { CancelMaskEditorPointer();CancelTexturePreviewPointer(); }
        }
        private void FinishMaskEditorPointer()
        {
            bool selecting=maskEditorDragging;int captured=maskEditorControl;
            CancelMaskEditorPointer();
            if(GUIUtility.hotControl==captured) GUIUtility.hotControl=0;
            if(browserGuiReleaseHotControl==captured) browserGuiReleaseHotControl=0;
            if(selecting) spriteMaskMessage=SpriteMaskRectangle.HasArea(maskEditorPoints) ? "Selection ready. Click Apply and close." : "Select a rectangle with a nonzero width and height.";
        }
        private void ResetMaskSelection(bool resetMessage=true)
        {
            CancelMaskEditorPointer();maskEditorPoints.Clear();maskEditorShowResult=false;maskEditorResultDirty=true;
            if(resetMessage) spriteMaskMessage="Select a rectangle with a nonzero width and height.";
        }
        private Vector2 MaskMousePoint(Rect rect,Vector2 mouse)
        { return new Vector2(Mathf.Clamp01((mouse.x-rect.x)/rect.width),Mathf.Clamp01(1-(mouse.y-rect.y)/rect.height)); }
        private void SetMaskDragEnd(Rect image,Vector2 mouse)
        {
            var end=MaskMousePoint(image,mouse);var start=maskEditorDragStart;
            maskEditorPoints.Clear();maskEditorPoints.AddRange(SpriteMaskRectangle.FromCorners(start.x,start.y,end.x,end.y));maskEditorResultDirty=true;
        }
        private void HandleMaskEditorMouse(Rect canvas,Rect image)
        {
            Event e=Event.current;if(e==null) return;
            int control=GUIUtility.GetControlID(MaskEditorControlHint,FocusType.Passive,canvas);
            if(HasMaskEditorPointer)
            {
                int button=maskEditorDragging ? 0 : 1;
                // rawType sees release even outside the clipped group or after another control used the event.
                if(e.rawType==EventType.MouseUp && e.button==button)
                {
                    if(maskEditorDragging) SetMaskDragEnd(image,e.mousePosition);else maskEditorPan+=e.delta;
                    FinishMaskEditorPointer();if(e.type!=EventType.Used) e.Use();return;
                }
                if(GUIUtility.hotControl!=maskEditorControl || (e.type==EventType.Repaint && !Input.GetMouseButton(button)))
                { FinishMaskEditorPointer();return; }
                if(e.type==EventType.MouseDrag && e.button==button)
                {
                    if(maskEditorDragging) SetMaskDragEnd(image,e.mousePosition);else maskEditorPan+=e.delta;
                    e.Use();return;
                }
                // A second mouse button or wheel must not replace the captured gesture.
                if(canvas.Contains(e.mousePosition) && (e.type==EventType.MouseDown || e.type==EventType.ScrollWheel)) e.Use();
                return;
            }
            if(!GUI.enabled) return;
            if(e.type==EventType.ScrollWheel&&canvas.Contains(e.mousePosition))
            {
                float previous=maskEditorZoom;maskEditorZoom=Mathf.Clamp(maskEditorZoom*Mathf.Pow(1.15f,-e.delta.y),1,8);
                float ratio=maskEditorZoom/previous;var center=new Vector2(canvas.width/2,canvas.height/2);
                maskEditorPan=e.mousePosition-center-(e.mousePosition-center-maskEditorPan)*ratio;
                if(maskEditorZoom==1) maskEditorPan=Vector2.zero;e.Use();return;
            }
            if(e.type!=EventType.MouseDown || !canvas.Contains(e.mousePosition) || GUIUtility.hotControl!=0) return;
            if(e.button==1)
            { maskEditorPanning=true;maskEditorControl=control;GUIUtility.hotControl=control;e.Use();return; }
            if(e.button==0 && !maskEditorShowResult)
            {
                maskEditorDragStart=MaskMousePoint(image,e.mousePosition);maskEditorDragging=true;
                // Keep the original captured ID until release; never overwrite it on Repaint.
                maskEditorControl=control;GUIUtility.hotControl=control;maskEditorPoints.Clear();maskEditorResultDirty=true;e.Use();
            }
        }
    }
}
