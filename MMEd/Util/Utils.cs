using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;

namespace MMEd.Util
{
  public abstract class Utils
  {
    /// <summary>
    ///  Converts a 16 bit PS colour into a 32 bit ARGB Color
    ///  structure
    /// 
    ///  Assumes that the top bit is a transparency flag, but
    ///  I don't yet have good evidence for this.
    /// </summary>
    public static int PS16bitColorToARGB(short xiVal)
    {
      int alpha = ((xiVal & 0x8000) != 0)
       ? 0 //transparent
       : unchecked((int)0xff000000); //solid

      //  {0}{bbbbb}gg} {ggg{rrrrr}  
      int r = ((xiVal << 3) & 0xf8);
      int g = ((xiVal >> 2) & 0xf8);
      int b = ((xiVal >> 7) & 0xf8);

      return ((r << 16) | (g << 8) | b) | alpha;
    }

    public static Color PSRGBColorToColor(int xiVal)
    {
      //ABGR => XRGB
      return Color.FromArgb((xiVal & unchecked((int)0xff000000)) | ((xiVal >> 16) & 0xff) | (xiVal & 0xff00) | ((xiVal << 16) & 0xff0000));
    }

    public static int ColorToPSRGBColor(Color xiVal)
    {
      //XRGB => ABGR
      return (int)((xiVal.A << 24) | (xiVal.B << 16) | (xiVal.G << 8) | (xiVal.R));
    }

    /// <summary>
    ///  Converts a 32 bit ARGB Color into a 16 bit PS colour
    ///  structure
    /// </summary>
    public static short ARGBColorToPS16bit(int xiVal)
    {
      int alpha;

      if ((xiVal & 0xff000000) == 0xff000000)
      {
        alpha = 0;
      }
      else if ((xiVal & 0xff000000) == 0x0)
      {
        alpha = 0x8000;
      }
      else
      {
        throw new ArgumentException("Can't do partially opaque colors");
      }

      int r = ((xiVal >> 19) & 0x1f);
      int g = ((xiVal >> 11) & 0x1f);
      int b = ((xiVal >> 3) & 0x1f);

      //  {0}{bbbbb}gg} {ggg{rrrrr}  
      return (short)(r | (g << 5) | (b << 10) | alpha);
    }

    public static GLTK.Point Short3CoordToPoint(Chunks.Short3Coord xiCoord)
    {
      return new GLTK.Point(xiCoord.X, xiCoord.Y, xiCoord.Z);
    }

    public static Chunks.Short3Coord PointToShort3Coord(GLTK.Point xiPoint)
    {
      return new Chunks.Short3Coord((short)xiPoint.x, (short)xiPoint.y, (short)xiPoint.z);
    }

    public static GLTK.Matrix Short3CoordToRotationMatrix(Chunks.Short3Coord xiCoord)
    {
      GLTK.Matrix lRotation = GLTK.Matrix.Rotation(-xiCoord.Z / 1024.0 * Math.PI / 2.0, GLTK.Vector.ZAxis);
      lRotation *= GLTK.Matrix.Rotation(-xiCoord.Y / 1024.0 * Math.PI / 2.0, GLTK.Vector.YAxis);
      lRotation *= GLTK.Matrix.Rotation(-xiCoord.X / 1024.0 * Math.PI / 2.0, GLTK.Vector.XAxis);

      return lRotation;
    }

    public static Chunks.Short3Coord RotationMatrixToShort3Coord(GLTK.Matrix xiRotation)
    {
      // Algorithm is as follows:
      //
      // There are only two degrees of freedom, so one of the coordinates in the
      // vector is redundant.  Therefore we fix one of the coordinates at 0 and
      // solve for the other 2.
      //
      //  * First identify the axis that moves the furthest under the rotation;
      //    this will be the fixed coordinate i.e. we'll rotate about the other
      //    two.
      //  * Next see what effect the rotation has on the axis that moves furthest;
      //    since rotations are defined uniquely by their action on a single point
      //    on the unit ball (apart from the poles) we just need to solve the
      //    simultaneous equations generated by applying rotations of theta and phi
      //    around the two non-fixed axis to this vector.  Note: it is sufficient
      //    to solve only two of the three equations as the third coordinate of the
      //    vector is determined by the other two.

      // see what effect the rotation has on the x and y axes
      GLTK.Vector lNewXAxis = xiRotation * GLTK.Vector.XAxis;
      GLTK.Vector lNewYAxis = xiRotation * GLTK.Vector.YAxis;
      GLTK.Vector lNewZAxis = xiRotation * GLTK.Vector.ZAxis;

      double lXdist = Math.Abs(lNewXAxis * GLTK.Vector.XAxis);
      double lYdist = Math.Abs(lNewYAxis * GLTK.Vector.YAxis);
      double lZdist = Math.Abs(lNewZAxis * GLTK.Vector.ZAxis);

      if (lXdist < lYdist && lXdist < lZdist)
      {
        // x axis has moved the furthest - use (0, y, z) format

        // rotate about z by theta first, then y by phi
        // need to solve cos(theta)*cos(phi) = newx.x; sin(theta) = newx.y
        double lTheta = Math.Asin(-lNewXAxis.y);

        // check which of the possible values for theta is correct
        if (lNewXAxis.x < 0) lTheta = Math.PI - lTheta;

        double lPhi = Math.Acos(lNewXAxis.x / Math.Cos(lTheta));

        // check which of the possible values for phi is correct
        if (lNewXAxis.z < 0) lPhi = -lPhi;

        return new Chunks.Short3Coord(
          0,
          (short)((lPhi / Math.PI) * 2 * 1024),
          (short)((lTheta / Math.PI) * 2 * 1024));
      }
      else if (lYdist < lZdist)
      {
        // y axis has moved the furthest - use (x, 0, z) format

        // rotate about z by theta first, then x by phi
        // need to solve -sin(theta) = newy.x; cos(theta)cos(phi) = newy.y
        double lTheta = Math.Asin(lNewYAxis.x);

        // check which of the possible values for theta is correct
        if (lNewYAxis.y < 0) lTheta = Math.PI - lTheta;

        double lPhi = Math.Acos(lNewYAxis.y / Math.Cos(lTheta));

        // check which of the possible values for phi is correct
        if (lNewYAxis.z > 0) lPhi = -lPhi;

        return new Chunks.Short3Coord(
          (short)((lPhi / Math.PI) * 2 * 1024),
          0,
          (short)((lTheta / Math.PI) * 2 * 1024));
      }
      else
      {
        // z axis has moved the furthest - use (x, y, 0) format

        // rotate about y by theta first, then x by phi
        // need to solve sin(theta) = newz.x; cos(phi)cos(theta) = newz.z
        double lTheta = Math.Asin(lNewZAxis.x);

        // check which of the possible values for theta is correct
        if (lNewZAxis.z < 0) lTheta = Math.PI - lTheta;

        double lPhi = Math.Acos(lNewZAxis.z / Math.Cos(lTheta));

        // check which of the possible values for phi is correct
        if (lNewZAxis.y < 0) lPhi = -lPhi;

        return new Chunks.Short3Coord(
          (short)((lPhi / Math.PI) * 2 * 1024),
          (short)((lTheta / Math.PI) * 2 * 1024),
          0);
      }
    }

    /// <summary>
    ///  I can't believe I have to write this.
    ///  There _must_ be a library function to do this...
    /// 
    ///  Adds slashes to a string to make it a valid C# string
    ///  literal.
    /// </summary>
    public static string EscapeString(string s)
    {
      int lFirstBadChar = -1;
      for (int i = 0; i < s.Length; i++)
      {
        char c = s[i];
        if ((c == '\\' || c < '!' || c > '~') && c != ' ')
        {
          lFirstBadChar = i;
          break;
        }
      }
      if (lFirstBadChar == -1) return s;
      StringBuilder acc = new StringBuilder(s.Length + 1);
      if (lFirstBadChar > 0)
      {
        acc.Append(s, 0, lFirstBadChar);
      }
      for (int i = lFirstBadChar; i < s.Length; i++)
      {
        char c = s[i];
        if ((c == '\\' || c < '!' || c > '~') && c != ' ')
        {
          switch (c)
          {
            case '\\': acc.Append("\\\\"); break;
            case '\r': acc.Append("\\r"); break;
            case '\n': acc.Append("\\n"); break;
            case '\t': acc.Append("\\t"); break;
            case '\0': acc.Append("\\0"); break;
            default:
              acc.AppendFormat("\\u{0:x4}", (int)c);
              break;
          }
        }
        else
        {
          acc.Append(c);
        }
      }
      return acc.ToString();
    }

    //counts how many objects in the given collection are of the
    //given type. Inefficient, but sometimes convenient;
    public static int CountInstances(System.Collections.IEnumerable xiCollection, Type xiType)
    {
      int acc = 0;
      foreach (object o in xiCollection)
      {
        if (xiType.IsInstanceOfType(o))
        {
          acc++;
        }
      }
      return acc;
    }

    public static bool ArrayCompare(Array xiArray1, Array xiArray2)
    {
      if (xiArray1 == null || xiArray2 == null)
      {
        return xiArray1 == xiArray2;
      }
      if (xiArray1.Length != xiArray2.Length)
      {
        return false;
      }
      for (int i = 0; i < xiArray1.Length; i++)
      {
        object lObj1 = xiArray1.GetValue(i);
        object lObj2 = xiArray2.GetValue(i);
        if (lObj1 is Array && lObj2 is Array)
        {
          if (!ArrayCompare((Array)lObj1, (Array)lObj2))
          {
            return false;
          }
        }
        else if (!lObj1.Equals(lObj2))
        {
          return false;
        }
      }
      return true;
    }

    public static string CamelCaseToSentence(string xiVal)
    {
      StringBuilder lAcc = new StringBuilder();
      bool lFirstLetter = true;
      foreach (char c in xiVal.ToCharArray())
      {
        if (char.IsUpper(c))
        {
          if (lFirstLetter)
          {
            lAcc.Append(c);
          }
          else
          {
            lAcc.Append(' ');
            lAcc.Append(char.ToLower(c));
          }
        }
        else
        {
          lAcc.Append(c);
        }
        lFirstLetter = false;
      }
      return lAcc.ToString();
    }

    public static void DrawArrow(
      Graphics xiGraphics,
      Pen xiPen,
      Point xiTip,
      int xiDirection,
      int xiLength,
      int xiHeadSize,
      bool xiCentre)
    {
      int lArrowHeadLength = 8 * xiHeadSize;
      double lAngle = Math.PI * ((double)xiDirection / 2048);

      if (xiCentre)
      {
        int xAdjust = (int)(Math.Sin(lAngle) * xiLength / 2);
        int yAdjust = -(int)(Math.Cos(lAngle) * xiLength / 2);
        xiTip.Offset(xAdjust, yAdjust);
      }

      Point lLineEnd = new Point(
        xiTip.X - (int)(xiLength * Math.Sin(lAngle)),
        xiTip.Y + (int)(xiLength * Math.Cos(lAngle)));

      double lArrowAngle1 = lAngle + (Math.PI / 6);
      double lArrowAngle2 = lAngle - (Math.PI / 6);
      Point lArrowHead1 = new Point(
        xiTip.X - (int)(lArrowHeadLength * Math.Sin(lArrowAngle1)),
        xiTip.Y + (int)(lArrowHeadLength * Math.Cos(lArrowAngle1)));
      Point lArrowHead2 = new Point(
        xiTip.X - (int)(lArrowHeadLength * Math.Sin(lArrowAngle2)),
        xiTip.Y + (int)(lArrowHeadLength * Math.Cos(lArrowAngle2)));

      xiGraphics.DrawLine(xiPen, xiTip, lLineEnd);
      xiGraphics.DrawPolygon(xiPen, new Point[] { xiTip, lArrowHead1, lArrowHead2 });
      xiGraphics.FillPolygon(xiPen.Brush, new Point[] { xiTip, lArrowHead1, lArrowHead2 });
    }

    public static void DrawCircle(
      Graphics xiGraphics,
      Pen xiPen,
      Point xiCentre,
      int xiRadius)
    {
      xiGraphics.DrawEllipse(
        xiPen,
        xiCentre.X - xiRadius,
        xiCentre.Y - xiRadius,
        xiRadius * 2,
        xiRadius * 2);
    }

    public static void DrawCross(
      Graphics xiGraphics,
      Pen xiPen,
      Point xiCentre,
      int xiLength)
    {
      xiGraphics.DrawLine(
        xiPen,
        xiCentre.X - xiLength,
        xiCentre.Y - xiLength,
        xiCentre.X + xiLength,
        xiCentre.Y + xiLength);
      xiGraphics.DrawLine(
        xiPen,
        xiCentre.X - xiLength,
        xiCentre.Y + xiLength,
        xiCentre.X + xiLength,
        xiCentre.Y - xiLength);
    }

    public static void DrawString(
      Graphics xiGraphics,
      string xiText,
      Point xiCentre)
    {
      Font lFont = new Font(FontFamily.GenericMonospace, 10);
      SizeF size = xiGraphics.MeasureString(xiText, lFont);

      float xf = xiCentre.X - size.Width / 2;
      float yf = xiCentre.Y - size.Height / 2;

      xiGraphics.FillRectangle(
        new SolidBrush(Color.White), 
        xf, 
        yf, 
        size.Width, 
        size.Height);

      xiGraphics.DrawString(
          xiText,
          lFont,
          new SolidBrush(Color.Black),
          xf,
          yf);
    }
  }
}
