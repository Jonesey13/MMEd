using System;
using System.Collections.Generic;
using System.Text;
using MMEd;
using MMEd.Chunks;
using System.Drawing;
using System.Windows.Forms;

namespace MMEd.Viewers
{
  public class BumpViewer : Viewer
  {
    private BumpViewer(MainForm xiMainForm) : base(xiMainForm) { }

    public override bool CanViewChunk(Chunk xiChunk)
    {
      return xiChunk is BumpImageChunk;
    }

    // Create an instance of the viewer manager class
    public static Viewer InitialiseViewer(MainForm xiMainForm)
    {
      return new BumpViewer(xiMainForm);
    }

    public override void SetSubject(Chunk xiChunk)
    {
      if (!(xiChunk is BumpImageChunk))
      {
        mChunk = null;
      }
      else
      {
        mChunk = (BumpImageChunk)xiChunk;
      }

      if (mLastSubject == mChunk)
      {
        return;
      }

      if (mChunk == null)
      {
        mMainForm.BumpEditPictureBox.Image = null;
      }
      else
      {
        mChunk.SetSelectedPixel(mX, mY);
        Image lImage = mChunk.ToImage();
        mMainForm.BumpEditPictureBox.Image = lImage;
        mMainForm.BumpViewPictureBox.Image = lImage;
        mScaleFactor = Math.Max(1, 128 / Math.Max(Math.Max(lImage.Width, lImage.Height), 1));
        if (mScaleFactor != 1)
        {
          mMainForm.BumpEditPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
          mMainForm.BumpEditPictureBox.Width = lImage.Width * mScaleFactor;
          mMainForm.BumpEditPictureBox.Height = lImage.Height * mScaleFactor;
          mMainForm.BumpViewPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
          mMainForm.BumpViewPictureBox.Width = lImage.Width * mScaleFactor;
          mMainForm.BumpViewPictureBox.Height = lImage.Height * mScaleFactor;
        }
        else
        {
          mMainForm.BumpEditPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
          mMainForm.BumpViewPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
        }

        BumpImageChunk.eBumpType lType = mChunk.GetPixelType(mX, mY);
        mMainForm.BumpTypeLabel.Text = lType.ToString();
        SetUpDropDown(lType);
      }

      mLastSubject = xiChunk;
    }

    protected void RefreshView()
    {
      Image lImage = mChunk.ToImage();
      mMainForm.BumpEditPictureBox.Image = lImage;
      mMainForm.BumpViewPictureBox.Image = lImage;
    }

    public override System.Windows.Forms.TabPage Tab
    {
      get { return mMainForm.ViewTabBump; }
    }

    protected void SetUpDropDown(BumpImageChunk.eBumpType xiSelected)
    {
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.plain);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.wall);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.milk);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.syrup);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.ketchup);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.roadBorder);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.roadBorder2);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.water);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown08);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown09);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown0A);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown0B);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown0C);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown0D);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown0E);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown0F);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown10);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown11);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown12);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown13);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown14);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown15);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.jumpWoosh);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown17);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown18);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown19);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown1A);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown1B);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown1C);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown1D);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown1E);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown1F);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown20);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown21);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown22);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.sand);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown24);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown25);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown26);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown27);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown28);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown29);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown2A);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown2B);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown2C);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown2D);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown2E);
      mMainForm.BumpCombo.Items.Add(BumpImageChunk.eBumpType.unknown2F);

      mMainForm.BumpCombo.SelectedItem = xiSelected;
    }

    public void BumpViewPictureBox_Click(object sender, EventArgs e)
    {
      MouseEventArgs lArgs = (MouseEventArgs)e;
      mX = lArgs.X / mScaleFactor;
      mY = lArgs.Y / mScaleFactor;
      mChunk.SetSelectedPixel(mX, mY);

      BumpImageChunk.eBumpType lType = mChunk.GetPixelType(mX, mY);
      mMainForm.BumpTypeLabel.Text = lType.ToString();
      
      RefreshView();
    }

    public void BumpEditPictureBox_Click(object sender, EventArgs e)
    {
      MouseEventArgs lArgs = (MouseEventArgs)e;
      mX = lArgs.X / mScaleFactor;
      mY = lArgs.Y / mScaleFactor;
      mChunk.SetSelectedPixel(mX, mY);

      BumpImageChunk.eBumpType lType = (BumpImageChunk.eBumpType)mMainForm.BumpCombo.SelectedItem;
      mChunk.SetPixelType(mX, mY, lType);
      mMainForm.BumpTypeLabel.Text = lType.ToString();

      RefreshView();
    }

    private int mScaleFactor;
    private BumpImageChunk mChunk;
    private Chunk mLastSubject = null;
    private int mX = 0;
    private int mY = 0;
  }
}
