using System;
using System.Collections.Generic;
using System.Text;
using MMEd;
using MMEd.Chunks;
using MMEd.Util;
using System.Drawing;
using System.Windows.Forms;
using GLTK;
using MMEd.Viewers.ThreeDee;


namespace MMEd.Viewers
{
  public class ThreeDeeViewer : Viewer
  {
    private double MOVE_SCALE = 100;

    private ThreeDeeViewer(MainForm xiMainForm)
      : base(xiMainForm)
    {
      mMainForm.KeyPreview = true;
      mMainForm.KeyPress += new KeyPressEventHandler(this.KeyPressHandle);
      mMainForm.FormClosing += new FormClosingEventHandler(mMainForm_FormClosing);
      mMainForm.Viewer3DRenderingSurface.MouseDown += new MouseEventHandler(Viewer3DRenderingSurface_MouseDown);
      mMainForm.Viewer3DRenderingSurface.MouseUp += new MouseEventHandler(Viewer3DRenderingSurface_MouseUp);
      mMainForm.Viewer3DRenderingSurface.MouseMove += new MouseEventHandler(Viewer3DRenderingSurface_MouseMove);
      mMainForm.ChunkTreeView.NodeMouseClick += new TreeNodeMouseClickEventHandler(ChunkTreeView_NodeMouseClick);

      mRenderer = new ImmediateModeRenderer();
      mRenderer.Attach(mMainForm.Viewer3DRenderingSurface);

      mScene = new Scene();
      mCamera = new Camera(80, 0.1, 1e10);
      mView = new GLTK.View(mScene, mCamera, mRenderer);
      mLight = new Light();
      mLight.DiffuseIntensity = 0.1;
      mLight.SpecularIntensity = 0.02;

      //add view mode menus:
      mOptionsMenu = new ToolStripMenuItem("3D");
      //
      PropertyController lMoveCtrl = new PropertyController(this, "MovementMode");
      mOptionsMenu.DropDownItems.AddRange(lMoveCtrl.CreateMenuItems());
      mOptionsMenu.DropDownItems.Add(new ToolStripSeparator());
      //
      PropertyController lLightCtrl = new PropertyController(this, "LightingMode");
      mOptionsMenu.DropDownItems.AddRange(lLightCtrl.CreateMenuItems());
      mOptionsMenu.DropDownItems.Add(new ToolStripSeparator());
      //
      PropertyController lNormCtrl = new PropertyController(this, "DrawNormalsMode");
      mOptionsMenu.DropDownItems.AddRange(lNormCtrl.CreateMenuItems());
      mOptionsMenu.DropDownItems.Add(new ToolStripSeparator());
      //
      PropertyController lTexModeCtrl = new PropertyController(this, "TextureMode");
      mOptionsMenu.DropDownItems.AddRange(lTexModeCtrl.CreateMenuItems());
      mOptionsMenu.DropDownItems.Add(new ToolStripSeparator());
      //
      PropertyController lSelMetaCtrl = new PropertyController(this, "SelectedMetadata");
      mOptionsMenu.DropDownItems.Add(lSelMetaCtrl.CreateToolStripComboBox());
      mOptionsMenu.DropDownItems.Add(new ToolStripSeparator());
      //
      mOptionsMenu.DropDownItems.Add(new ToolStripMenuItem("Hide all Flats without FlgD", null, new EventHandler(this.HideAllFlatsWithoutFlgDClicked)));
      mMainForm.mMenuStrip.Items.Add(mOptionsMenu);
    }
    Light mLight;
    void ChunkTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
      RebuildScene();
      InvalidateViewer();
    }

    void Viewer3DRenderingSurface_MouseMove(object sender, MouseEventArgs e)
    {
      if (!mDraggingView || mSubject == null) return;
      System.Drawing.Point lNewMousePoint = e.Location;
      if (mDraggingButton == MouseButtons.Left)
      {
        bool lCameraIsUpsideDown = mCamera.YAxis.Dot(Vector.ZAxis) < 0;
        switch (MovementMode)
        {
          case eMovementMode.FlyMode:
            mCamera.Rotate(0.01 * (lNewMousePoint.X - mLastMouseDown.X), lCameraIsUpsideDown ? Vector.ZAxis : -Vector.ZAxis);
            mCamera.Rotate(0.01 * (lNewMousePoint.Y - mLastMouseDown.Y), mCamera.XAxis);
            break;
          case eMovementMode.InspectMode:
            mCamera.RotateAboutWorldOrigin(0.01 * (lNewMousePoint.X - mLastMouseDown.X), mCamera.YAxis);
            mCamera.RotateAboutWorldOrigin(0.01 * (lNewMousePoint.Y - mLastMouseDown.Y), mCamera.XAxis);
            break;
          default: throw new Exception("Unreachable case");
        }
      }
      else if (mDraggingButton == MouseButtons.Right)
      {
        switch (MovementMode)
        {
          case eMovementMode.FlyMode:
            mCamera.Move(-0.1 * MOVE_SCALE * (lNewMousePoint.X - mLastMouseDown.X) * mCamera.XAxis);
            mCamera.Move(0.1 * MOVE_SCALE * (lNewMousePoint.Y - mLastMouseDown.Y) * mCamera.ZAxis);
            break;
          case eMovementMode.InspectMode:
            Vector lStartPos = mCamera.Position.GetPositionVector();
            Vector lMoveVec = 0.1 * MOVE_SCALE * (lNewMousePoint.Y - mLastMouseDown.Y) * mCamera.ZAxis;
            //don't move through the origin
            if (lMoveVec.Dot(lStartPos) / lStartPos.LengthSquared > -1.0)
            {
              mCamera.Move(lMoveVec);
            }
            break;
          default: throw new Exception("Unreachable case");
        }
      }
      if (LightingMode == eLightingMode.Headlight)
      {
        //qq this replaces the matrix, rather than changes the values,
        // but that's OK for now
        mLight.Transform = mCamera.Transform;
      }
      mLastMouseDown = lNewMousePoint;
      InvalidateViewer();
    }

    void Viewer3DRenderingSurface_MouseUp(object sender, MouseEventArgs e)
    {
      mDraggingView = false;
      mMainForm.Viewer3DRenderingSurface.Capture = false;
    }

    bool mDraggingView = false;
    MouseButtons mDraggingButton;
    System.Drawing.Point mLastMouseDown;

    void Viewer3DRenderingSurface_MouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Middle)
      {
        Mesh lMesh = mRenderer.Pick(e.X, e.Y);
        if (lMesh != null)
        {
          if (lMesh is OwnedMesh && ((OwnedMesh)lMesh).Owner is Chunk)
          {
            OwnedMesh om = (OwnedMesh)lMesh;
            Chunk c = (Chunk)om.Owner;
            MessageBox.Show(string.Format("Clicked on {0} with name {1}", c.GetType().Name, c.Name));
          }
          else if (lMesh is OwnedMesh && ((OwnedMesh)lMesh).Owner is FlatChunk.ObjectEntry)
          {
            FlatChunk.ObjectEntry oe = (FlatChunk.ObjectEntry)((OwnedMesh)lMesh).Owner;
            MessageBox.Show(string.Format("Clicked on object type {0} at {1}, rotation {2}", oe.ObjtType, oe.OriginPosition, oe.RotationVector));
          }
          else if (lMesh is OwnedMesh && ((OwnedMesh)lMesh).Owner is FlatChunk.WeaponEntry)
          {
            FlatChunk.WeaponEntry we = (FlatChunk.WeaponEntry)((OwnedMesh)lMesh).Owner;
            MessageBox.Show(string.Format("Clicked on weapon type {0} at {1}", we.WeaponType, we.OriginPosition));
          }
          else
          {
            lMesh.RenderMode = lMesh.RenderMode == RenderMode.Filled ?
              RenderMode.Textured : RenderMode.Filled;
            InvalidateViewer();
          }
        }
      }
      else
      {
        mMainForm.Viewer3DRenderingSurface.Capture = true;
        mLastMouseDown = e.Location;
        mDraggingButton = e.Button;
        mDraggingView = true;
      }
    }

    void mMainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
      mMainForm.Viewer3DRenderingSurface.Release();
    }

    public void HideAllFlatsWithoutFlgDClicked(object xiSender, EventArgs xiArgs)
    {
      if (!(mSubject is Level))
      {
        MessageBox.Show("This action only applicable when viewing Levels");
        return;
      }
      foreach (FlatChunk f in ((Level)mSubject).SHET.Flats)
      {
        f.TreeNode.Checked = !f.FlgD;
      }
      ChunkTreeView_NodeMouseClick(null, null);
    }


    public override bool CanViewChunk(Chunk xiChunk)
    {
      return xiChunk is IEntityProvider;
    }

    // Create an instance of the viewer manager class
    public static Viewer InitialiseViewer(MainForm xiMainForm)
    {
      return new ThreeDeeViewer(xiMainForm);
    }

    private IEntityProvider mSubject = null;

    Scene mScene;
    Camera mCamera;
    ImmediateModeRenderer mRenderer;
    GLTK.View mView;

    ToolStripMenuItem mOptionsMenu;

    public override void SetSubject(Chunk xiChunk)
    {
      if (!(xiChunk is IEntityProvider)) xiChunk = null;
      mOptionsMenu.Visible = (xiChunk != null);
      if (mSubject == xiChunk) return;
      bool lResetViewMode = true;
      if (xiChunk != null && mSubject != null && xiChunk.GetType() == mSubject.GetType())
        lResetViewMode = false;
      mSubject = (IEntityProvider)xiChunk;

      const double MOVE_SCALE = 100;

      Cursor prevCursor = mMainForm.Viewer3DRenderingSurface.Cursor;
      mMainForm.Viewer3DRenderingSurface.Cursor = Cursors.WaitCursor;
      RebuildScene();
      if (mSubject != null)
      {
        mCamera.Position = new GLTK.Point(-3 * MOVE_SCALE, -3 * MOVE_SCALE, 3 * MOVE_SCALE);
        mCamera.LookAt(new GLTK.Point(3 * MOVE_SCALE, 3 * MOVE_SCALE, 0), new GLTK.Vector(0, 0, 1));

        //set defaults
        if (lResetViewMode)
        {
          if (mSubject is TMDChunk)
          {
            LightingMode = eLightingMode.None; //qq
            MovementMode = eMovementMode.InspectMode;
            DrawNormalsMode = eDrawNormalsMode.HideNormals;
            TextureMode = eTextureMode.NormalTextures;
            SelectedMetadata = eTexMetaDataEntries.Waypoint;
          }
          else
          {
            LightingMode = eLightingMode.None;
            MovementMode = eMovementMode.FlyMode;
            DrawNormalsMode = eDrawNormalsMode.HideNormals;
            TextureMode = eTextureMode.NormalTextures;
            SelectedMetadata = eTexMetaDataEntries.Waypoint;
          }
        }

        if (MovementMode == eMovementMode.InspectMode)
        {
          mLight.Transform = mCamera.Transform;
        }

        mMainForm.ChunkTreeView.CheckBoxes = (mSubject is Level);
      }
      else
      {
        if (mMainForm.ViewerTabControl.SelectedTab == null
          || !(mMainForm.ViewerTabControl.SelectedTab.Tag is ThreeDeeEditor))
        {
          mMainForm.ChunkTreeView.CheckBoxes = false;
        }
      }
      mMainForm.Viewer3DRenderingSurface.Cursor = prevCursor;

      InvalidateViewer();
    }

    private void RebuildScene()
    {
      mScene.Clear();
      if (mSubject != null)
        mScene.AddRange(mSubject.GetEntities(mMainForm.Level, TextureMode, SelectedMetadata));
    }

    public static GLTK.Point Short3CoordToPoint(Short3Coord xiVal)
    {
      return new GLTK.Point(xiVal.X, xiVal.Y, xiVal.Z);
    }

    public override System.Windows.Forms.TabPage Tab
    {
      get { return mMainForm.ViewTab3D; }
    }

    #region MovementMode property

    private eMovementMode mMovementMode;
    public eMovementMode MovementMode
    {
      get { return mMovementMode; }
      set
      {
        mMovementMode = value;
        if (value == eMovementMode.InspectMode)
        {
          mCamera.LookAt(GLTK.Point.Origin, mCamera.YAxis);
        }
        if (OnMovementModeChanged != null) OnMovementModeChanged(this, null);
        InvalidateViewer();
      }
    }
    public event EventHandler OnMovementModeChanged;

    #endregion

    #region DrawNormalsMode property

    private eDrawNormalsMode mDrawNormalsMode;
    public eDrawNormalsMode DrawNormalsMode
    {
      get { return mDrawNormalsMode; }
      set
      {
        mDrawNormalsMode = value;
        if (OnDrawNormalsModeChanged != null) OnDrawNormalsModeChanged(this, null);
        InvalidateViewer();
      }
    }
    public event EventHandler OnDrawNormalsModeChanged;

    #endregion

    #region LightingMode property

    private eLightingMode mLightingMode;
    public eLightingMode LightingMode
    {
      get { return mLightingMode; }
      set
      {
        switch (value)
        {
          case eLightingMode.None:
            mRenderer.DisableLighting();
            break;
          case eLightingMode.Headlight:
            mRenderer.EnableLighting();
            mRenderer.ResetLights();
            mLight.Transform = mCamera.Transform;
            mScene.AddLight(mLight);
            break;
          case eLightingMode.OverheadLight:
            MessageBox.Show("OverheadLight mode not supported yet");
            return;
        }
        mLightingMode = value;
        if (OnLightingModeChanged != null) OnLightingModeChanged(this, null);
        InvalidateViewer();
      }
    }
    public event EventHandler OnLightingModeChanged;

    #endregion

    #region TextureMode property

    private eTextureMode mTextureMode;
    public eTextureMode TextureMode
    {
      get { return mTextureMode; }
      set
      {
        mTextureMode = value;
        if (OnTextureModeChanged != null) OnTextureModeChanged(this, null);
        RebuildScene();
        InvalidateViewer();
      }
    }
    public event EventHandler OnTextureModeChanged;

    #endregion

    #region SelectedMetadata property

    private eTexMetaDataEntries mSelectedMetadata;

    public eTexMetaDataEntries SelectedMetadata
    {
      get { return mSelectedMetadata; }
      set
      {
        mSelectedMetadata = value;
        if (OnSelectedMetadataChanged != null) OnSelectedMetadataChanged(this, null);
        RebuildScene();
        InvalidateViewer();
      }
    }

    public event EventHandler OnSelectedMetadataChanged;

    #endregion

    private void InvalidateViewer()
    {
      mMainForm.Viewer3DRenderingSurface.Invalidate();
    }

    // a 2d move request, which will be turned into 3d camera
    // movement, in a manner dependent on
    private void MoveCamera(Vector xiMove)
    {
      if (MovementMode == eMovementMode.InspectMode)
      {
        //turn movement requests into rotation about origin

      }

      if (LightingMode == eLightingMode.Headlight)
      {
        //light moves with camera
        mLight.Move(xiMove);
      }
      mCamera.Move(xiMove);
    }

    private void KeyPressHandle(object sender, KeyPressEventArgs e)
    {
      if (mMainForm.ViewerTabControl.SelectedTab != Tab) return;

      switch (e.KeyChar)
      {
        case 'W':
        case 'w':
          mCamera.Move(-1.0 * mCamera.ZAxis * MOVE_SCALE);
          break;

        case 'S':
        case 's':
          mCamera.Move(1.0 * mCamera.ZAxis * MOVE_SCALE);
          break;

        case 'A':
        case 'a':
          mCamera.Move(-1.0 * mCamera.XAxis * MOVE_SCALE);
          break;

        case 'D':
        case 'd':
          mCamera.Move(1.0 * mCamera.XAxis * MOVE_SCALE);
          break;

        case 'Q':
        case 'q':
          mCamera.Move(-1.0 * mCamera.ZAxis * MOVE_SCALE);
          break;

        case 'E':
        case 'e':
          mCamera.Move(1.0 * mCamera.ZAxis * MOVE_SCALE);
          break;

        case 'I':
        case 'i':
          mCamera.Rotate(-0.1, mCamera.XAxis);
          break;

        case 'K':
        case 'k':
          mCamera.Rotate(0.1, mCamera.XAxis);
          break;

        case 'J':
        case 'j':
          mCamera.Rotate(-0.1, mCamera.YAxis);
          break;

        case 'L':
        case 'l':
          mCamera.Rotate(0.1, mCamera.YAxis);
          break;

        case 'U':
        case 'u':
          mCamera.Rotate(-0.1, mCamera.ZAxis);
          break;

        case 'O':
        case 'o':
          mCamera.Rotate(0.1, mCamera.ZAxis);
          break;
      }

      InvalidateViewer();
    }
  }
}
