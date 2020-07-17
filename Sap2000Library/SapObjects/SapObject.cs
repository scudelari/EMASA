using System;
using System.Collections.Generic;
using Sap2000Library.Managers;
using SAP2000v1;

namespace Sap2000Library.SapObjects
{
    public class SapObject
    {
        public string Name { get; set; }
        public SapObjectType SapType;
        protected SapManagerBase b_owner;

        internal SapObject(string name, SapObjectType type, SapManagerBase inManBase)
        {
            Name = name;
            SapType = type;
            b_owner = inManBase;
        }

        public override bool Equals(object obj)
        {
            if (obj is SapObject inObject)
            {
                if (Name == inObject.Name && SapType == inObject.SapType) return true;
                else return false;
            }

            return false;
        }
        public static bool operator ==(SapObject lhs, SapObject rhs)
        {

            // If left hand side is null...
            if (ReferenceEquals(lhs, null))
            {
                // ...and right hand side is null...
                if (ReferenceEquals(rhs, null))
                {
                    //...both are null and are Equal.
                    return true;
                }

                // ...right hand side is not null, therefore not Equal.
                return false;
            }

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }
        public static bool operator !=(SapObject lhs, SapObject rhs)
        {
            return !(lhs == rhs);
        }
        public override int GetHashCode()
        {
            return (Name, SapType).GetHashCode();
        }

        public bool Select()
        {
            int ret = 0;
            switch (SapType)
            {
                case SapObjectType.Point:
                    ret = b_owner.SapApi.PointObj.SetSelected(Name, true, eItemType.Objects);
                    break;
                case SapObjectType.Frame:
                    ret = b_owner.SapApi.FrameObj.SetSelected(Name, true, eItemType.Objects);
                    break;
                case SapObjectType.Link:
                    ret = b_owner.SapApi.LinkObj.SetSelected(Name, true, eItemType.Objects);
                    break;
                case SapObjectType.Cable:
                    ret = b_owner.SapApi.CableObj.SetSelected(Name, true, eItemType.Objects);
                    break;
                case SapObjectType.Area:
                    ret = b_owner.SapApi.AreaObj.SetSelected(Name, true, eItemType.Objects);
                    break;
                case SapObjectType.Solid:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
                case SapObjectType.Tendon:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
                default:
                    break;
            }
            return ret == 0;
        }
        public bool SelectJoints()
        {
            int ret = 0;
            switch (SapType)
            {
                case SapObjectType.Point:
                    break;
                case SapObjectType.Frame:
                    if (!((SapLine)this).iEndPoint.Select()) return false;
                    if (!((SapLine)this).jEndPoint.Select()) return false;
                    break;
                case SapObjectType.Link:
                    if (!((SapLine)this).iEndPoint.Select()) return false;
                    if (!((SapLine)this).jEndPoint.Select()) return false;
                    break;
                case SapObjectType.Cable:
                    if (!((SapLine)this).iEndPoint.Select()) return false;
                    if (!((SapLine)this).jEndPoint.Select()) return false;
                    break;
                case SapObjectType.Area:
                    foreach (SapPoint pnt in ((SapArea)this).Points) if (!pnt.Select()) return false;
                    break;
                case SapObjectType.Solid:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
                case SapObjectType.Tendon:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
                default:
                    break;
            }
            return ret == 0;
        }
        public virtual bool ChangeConnectivity(SapPoint oldPoint, SapPoint newPoint)
        {
            throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
        }
        /// <summary>
        /// Changes the name of the object.
        /// </summary>
        /// <param name="newName">The new desired name.</param>
        /// <exception cref="S2KHelperException">Thrown if could not rename the object.</exception>
        public void ChangeName(string newName)
        {
            int ret = -1;
            switch (SapType)
            {
                case SapObjectType.Point:
                    ret = b_owner.SapApi.PointObj.ChangeName(Name, newName);
                    break;
                case SapObjectType.Frame:
                    ret = b_owner.SapApi.FrameObj.ChangeName(Name, newName);
                    break;
                case SapObjectType.Link:
                    ret = b_owner.SapApi.LinkObj.ChangeName(Name, newName);
                    break;
                case SapObjectType.Cable:
                    ret = b_owner.SapApi.CableObj.ChangeName(Name, newName);
                    break;
                case SapObjectType.Area:
                    ret = b_owner.SapApi.AreaObj.ChangeName(Name, newName);
                    break;
                case SapObjectType.Solid:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
                    break;
                case SapObjectType.Tendon:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.", this);
                    break;
                default:
                    break;
            }

            if (ret == 0)
            {
                Name = newName;
                return;
            }
            else throw new S2KHelperException($"Could not rename object {Name} to {newName}.");
        }

        private List<string> _groups = null;
        public List<string> Groups
        {
            get
            {
                if (_groups == null)
                {
                    int numberGroups = 0;
                    string[] groups = null;

                    int ret = -1;
                    switch (SapType)
                    {
                        case SapObjectType.Point:
                            ret = b_owner.SapApi.PointObj.GetGroupAssign(Name, ref numberGroups, ref groups);
                            break;
                        case SapObjectType.Frame:
                            ret = b_owner.SapApi.FrameObj.GetGroupAssign(Name, ref numberGroups, ref groups);
                            break;
                        case SapObjectType.Link:
                            ret = b_owner.SapApi.LinkObj.GetGroupAssign(Name, ref numberGroups, ref groups);
                            break;
                        case SapObjectType.Cable:
                            ret = b_owner.SapApi.CableObj.GetGroupAssign(Name, ref numberGroups, ref groups);
                            break;
                        case SapObjectType.Area:
                            ret = b_owner.SapApi.AreaObj.GetGroupAssign(Name, ref numberGroups, ref groups);
                            break;
                        case SapObjectType.Solid:
                            throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.");
                            break;
                        case SapObjectType.Tendon:
                            throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.");
                            break;
                        default:
                            break;
                    }

                    // Sets the name into the object
                    if (ret != 0) return null;


                    _groups = new List<string>();

                    foreach (string item in groups)
                    {
                        _groups.Add(item);
                    }
                }

                return _groups;
            }
        }

        /// <summary>
        /// Adds the object to the Group.
        /// </summary>
        /// <param name="groupName">The name of the group to which the object will be added.</param>
        /// <exception cref="S2KHelperException">Thrown when the object could not be added to the group.</exception>
        public void AddGroup(string groupName)
        {
            int ret = -1;
            switch (SapType)
            {
                case SapObjectType.Point:
                    ret = b_owner.SapApi.PointObj.SetGroupAssign(Name, groupName, false, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not add the joint {Name} to group {groupName}.");
                    break;
                case SapObjectType.Frame:
                    ret = b_owner.SapApi.FrameObj.SetGroupAssign(Name, groupName, false, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not add the frame {Name} to group {groupName}.");
                    break;
                case SapObjectType.Link:
                    ret = b_owner.SapApi.LinkObj.SetGroupAssign(Name, groupName, false, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not add the link {Name} to group {groupName}.");
                    break;
                case SapObjectType.Cable:
                    ret = b_owner.SapApi.CableObj.SetGroupAssign(Name, groupName, false, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not add the cable {Name} to group {groupName}.");
                    break;
                case SapObjectType.Area:
                    ret = b_owner.SapApi.AreaObj.SetGroupAssign(Name, groupName, false, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not add the area {Name} to group {groupName}.");
                    break;
                case SapObjectType.Solid:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.");
                case SapObjectType.Tendon:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.");
                default:
                    break;
            }
            _groups = null;
        }
        /// <summary>
        /// Adds the object to the groups
        /// </summary>
        /// <param name="inGroups">The list of groups to which the object will be added.</param>
        /// <returns>True if added to all groups. False if could not add to one of the groups.</returns>
        public bool AddGroups(List<string> inGroups)
        {
            bool allOk = true;
            foreach (string item in inGroups)
            {
                try
                {
                    AddGroup(item);
                }
                catch (Exception)
                {
                    allOk = false;
                }
            }
            return allOk;
        }
        public bool CopyGroupsFrom(SapObject fromObject)
        {
            return AddGroups(fromObject.Groups);
        }
        public void RemoveGroup(string groupName)
        {
            int ret = -1;
            switch (SapType)
            {
                case SapObjectType.Point:
                    ret = b_owner.SapApi.PointObj.SetGroupAssign(Name, groupName, true, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not remove the joint {Name} from group {groupName}.");
                    break;
                case SapObjectType.Frame:
                    ret = b_owner.SapApi.FrameObj.SetGroupAssign(Name, groupName, true, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not remove the frame {Name} from group {groupName}.");
                    break;
                case SapObjectType.Link:
                    ret = b_owner.SapApi.LinkObj.SetGroupAssign(Name, groupName, true, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not remove the link {Name} from group {groupName}.");
                    break;
                case SapObjectType.Cable:
                    ret = b_owner.SapApi.CableObj.SetGroupAssign(Name, groupName, true, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not remove the cable {Name} from group {groupName}.");
                    break;
                case SapObjectType.Area:
                    ret = b_owner.SapApi.AreaObj.SetGroupAssign(Name, groupName, true, eItemType.Objects);
                    if (ret != 0) throw new S2KHelperException($"Could not remove the area {Name} from group {groupName}.");
                    break;
                case SapObjectType.Solid:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.");
                    break;
                case SapObjectType.Tendon:
                    throw new S2KHelperException($"Type {(SapType).ToString()} is still not supported in this method. Please write the code.");
                    break;
                default:
                    break;
            }

            _groups = null;
        }

    }
}
