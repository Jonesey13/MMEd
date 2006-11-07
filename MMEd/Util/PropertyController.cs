using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

// creates Windows.Forms controls to display and update enumerated properties

namespace MMEd.Util
{
  public class PropertyController
  {
    private object mTarget;
    private PropertyInfo mProperty;
    private NamedValueHolder[] mAllowedValues;

    public PropertyController(
        object xiTarget,
        string xiPropertyName)
      : this(xiTarget, xiPropertyName, null, null) { }

    public PropertyController(
        object xiTarget,
        string xiPropertyName,
        string xiPropertyUpdateEventName)
      : this (xiTarget, xiPropertyName, xiPropertyUpdateEventName, null) {}

    public PropertyController(
        object xiTarget,
        string xiPropertyName,
        NamedValueHolder[] xiAllowedValues)
      : this(xiTarget, xiPropertyName, null, xiAllowedValues) { }

    public PropertyController(
        object xiTarget,
        string xiPropertyName,
        string xiPropertyUpdateEventName,
        NamedValueHolder[] xiAllowedValues)
    {
      mTarget = xiTarget;
      mProperty = mTarget.GetType().GetProperty(xiPropertyName);
      if (mProperty == null) throw new ArgumentException(string.Format("Property \"{0}\" not found on type {1}", xiPropertyName, xiTarget.GetType()));
      Type lPropType = mProperty.PropertyType;
      mAllowedValues = xiAllowedValues;

      if (!lPropType.IsEnum 
        && lPropType != typeof(bool)
        && xiAllowedValues == null) throw new ArgumentException("If property is not enumerated or boolean, then you must provide an array of allowed values");

      if (xiPropertyUpdateEventName != null)
      {
        EventInfo lEv = mTarget.GetType().GetEvent(xiPropertyUpdateEventName);
        lEv.AddEventHandler(mTarget, new EventHandler(this.ValueChangeHandler));
      }
    }

    
    private ToolStripMenuItem[] mToolStripItems;
    public ToolStripMenuItem[] CreateMenuItems()
    {
      if (mToolStripItems != null) throw new Exception("Can't create more than one group of menu items");
      List<ToolStripMenuItem> lAcc = new List<ToolStripMenuItem>();
      if (mAllowedValues != null)
      {
        foreach (NamedValueHolder lNV in mAllowedValues)
        {
          ToolStripMenuItem lItem = new ToolStripMenuItem();
          lItem.Text = lNV.Name;
          lItem.Tag = lNV.Value;
          lItem.Click += new EventHandler(this.ToolStripClickHandler);
          lAcc.Add(lItem);
        }
      }
      else if (mProperty.PropertyType.IsEnum)
      {
        foreach (string lValue in Enum.GetNames(mProperty.PropertyType))
        {
          ToolStripMenuItem lItem = new ToolStripMenuItem();
          lItem.Text = Utils.CamelCaseToSentence(lValue);
          lItem.Tag = lValue;
          lItem.Click += new EventHandler(this.ToolStripClickHandler);
          lAcc.Add(lItem);
        }
      }
      else //bool
      {
        ToolStripMenuItem lItem = new ToolStripMenuItem();
        lItem.Text = Utils.CamelCaseToSentence(mProperty.Name);
        lItem.Click += new EventHandler(this.ToolStripClickHandler);
        lAcc.Add(lItem);
      }
      mToolStripItems = lAcc.ToArray();
      ValueChangeHandler(null, null);
      return mToolStripItems;
    }

    public class NamedValueHolder
    {
      public object Value;
      public string Name;
      public override string ToString()
      {
        return Name;
      }
      public NamedValueHolder(string xiName, object xiValue)
      {
        this.Value = xiValue;
        this.Name = xiName;
      }
      public override bool Equals(object obj)
      {
        return Value.Equals(((NamedValueHolder)obj).Value);
      }
      public override int GetHashCode()
      {
        return Value.GetHashCode();
      }

    }

    private ToolStripComboBox mToolStripComboBox;
    public ToolStripComboBox CreateToolStripComboBox()
    {
      if (mToolStripComboBox != null) throw new Exception("Can't create more than one menu combo box");
      if (!mProperty.PropertyType.IsEnum && mAllowedValues == null) throw new Exception("Only enumerated properties or properties with a list of allowed values can use menu combo box");
      mToolStripComboBox = new ToolStripComboBox();

      if (mAllowedValues != null)
      {
        foreach (NamedValueHolder lNV in mAllowedValues)
        {
          mToolStripComboBox.Items.Add(lNV);
        }
      }
      else
      {
        foreach (object lValue in Enum.GetValues(mProperty.PropertyType))
        {
          mToolStripComboBox.Items.Add(new NamedValueHolder(Utils.CamelCaseToSentence(Enum.GetName(mProperty.PropertyType, lValue)), lValue));
        }
      }

      mToolStripComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      mToolStripComboBox.SelectedIndexChanged += new EventHandler(this.ComboBoxSelectHandler);
      ValueChangeHandler(null, null);
      return mToolStripComboBox;
    }

    public void ComboBoxSelectHandler(object xiSender, EventArgs xiArgs)
    {
      object lNewValue = ((NamedValueHolder)((ToolStripComboBox)xiSender).SelectedItem).Value;
      mProperty.SetValue(mTarget, lNewValue, null);
      ValueChangeHandler(null, null);
    }

    public void ToolStripClickHandler(object xiSender, EventArgs xiArgs)
    {
      object lNewValue;
      if (mAllowedValues != null)
      {
        lNewValue = ((ToolStripMenuItem)xiSender).Tag;
      }
      else if (mProperty.PropertyType.IsEnum)
      {
        string lNewValueStr = (string)((ToolStripMenuItem)xiSender).Tag;
        lNewValue = Enum.Parse(mProperty.PropertyType, lNewValueStr);
      }
      else
      {
        lNewValue = !(((ToolStripMenuItem)xiSender).Checked);
      }
      mProperty.SetValue(mTarget, lNewValue, null);
      ValueChangeHandler(null, null);
    }

    public void ValueChangeHandler(object xiSender, EventArgs xiArgs)
    {
      if (mAllowedValues != null)
      {
        object lNewValue = mProperty.GetValue(mTarget, null);
        if (mToolStripItems != null)
        {
          foreach (ToolStripMenuItem t in mToolStripItems)
            t.Checked = (t.Tag != null && t.Tag.Equals(lNewValue));
        }
        if (mToolStripComboBox != null)
        {
          mToolStripComboBox.SelectedItem = new NamedValueHolder(null, lNewValue);
        }
      } 
      else if (mProperty.PropertyType.IsEnum)
      {
        object lNewValue = mProperty.GetValue(mTarget, null);
        string lNewValueStr = Enum.GetName(mProperty.PropertyType, lNewValue);
        if (mToolStripItems != null)
        {
          foreach (ToolStripMenuItem t in mToolStripItems)
            t.Checked = ((string)t.Tag == lNewValueStr);
        }
        if (mToolStripComboBox != null)
        {
          mToolStripComboBox.SelectedItem = new NamedValueHolder(null, lNewValue);
        }
      }
      else
      {
        mToolStripItems[0].Checked = (bool)mProperty.GetValue(mTarget, null);
      }
    }
  }
}
