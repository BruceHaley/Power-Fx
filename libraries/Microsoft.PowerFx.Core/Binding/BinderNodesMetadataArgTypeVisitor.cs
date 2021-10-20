﻿//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


using Microsoft.AppMagic.Authoring.DataToControls;
using Microsoft.PowerFx.Core.App;

namespace Microsoft.AppMagic.Authoring.Texl
{
    internal sealed partial class TexlBinding
    {
        private sealed class BinderNodesMetadataArgTypeVisitor : Visitor
        {
            private TexlBinding _txb;

            public BinderNodesMetadataArgTypeVisitor(TexlBinding binding, INameResolver resolver, DType topScope, bool useThisRecordForRuleScope)
                : base(binding, resolver, topScope, useThisRecordForRuleScope)
            {
                Contracts.AssertValue(binding);

                _txb = binding;
            }

            private bool IsColumnMultiChoice(IExternalColumnMetadata columnMetadata)
            {
                Contracts.AssertValue(columnMetadata);

                return (columnMetadata?.DataFormat == DataFormat.Lookup);
            }

            public override void PostVisit(DottedNameNode node)
            {
                Contracts.AssertValue(node);

                DType lhsType = _txb.GetType(node.Left);
                DType typeRhs = DType.Invalid;
                DName nameRhs = node.Right.Name;
                FirstNameInfo firstNameInfo;
                FirstNameNode firstNameNode;
                IExternalTableMetadata tableMetadata;
                DType nodeType = DType.Unknown;

                if (node.Left.Kind != NodeKind.FirstName &&
                    node.Left.Kind != NodeKind.DottedName)
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName);
                    return;
                }

                nameRhs = GetLogicalNodeNameAndUpdateDisplayNames(lhsType, node.Right);

                if (!lhsType.TryGetType(nameRhs, out typeRhs))
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName);
                    return;
                }

                // There are two cases:
                // 1. RHS could be an option set.
                // 2. RHS could be a data entity.
                // 3. RHS could be a column name and LHS would be a datasource.
                if (typeRhs.IsOptionSet)
                {
                    nodeType = typeRhs;
                }
                else if (typeRhs.IsExpandEntity)
                {
                    var entityInfo = typeRhs.ExpandInfo;
                    Contracts.AssertValue(entityInfo);

                    string entityPath = string.Empty;
                    if (lhsType.HasExpandInfo)
                        entityPath = lhsType.ExpandInfo.ExpandPath.ToString();

                    DType expandedEntityType = GetExpandedEntityType(typeRhs, entityPath);

                    var parentDataSource = entityInfo.ParentDataSource;
                    var metadata = new DataTableMetadata(parentDataSource.Name, parentDataSource.Name);
                    nodeType = DType.CreateMetadataType(new DataColumnMetadata(typeRhs.ExpandInfo.Name, expandedEntityType, metadata));
                }
                else if ((firstNameNode = node.Left.AsFirstName()) != null && (firstNameInfo = _txb.GetInfo(firstNameNode)) != null)
                {
                    var tabularDataSourceInfo = firstNameInfo.Data as IExternalTabularDataSource;
                    tableMetadata = tabularDataSourceInfo?.TableMetadata;
                    if (tableMetadata == null || !tableMetadata.TryGetColumn(nameRhs.Value, out var columnMetadata) || !IsColumnMultiChoice(columnMetadata))
                    {
                        SetDottedNameError(node, TexlStrings.ErrInvalidName);
                        return;
                    }

                    var metadata = new DataTableMetadata(tabularDataSourceInfo.Name, tableMetadata.DisplayName);
                    nodeType = DType.CreateMetadataType(new DataColumnMetadata(columnMetadata, metadata));
                }
                else
                {
                    SetDottedNameError(node, TexlStrings.ErrInvalidName);
                    return;
                }

                Contracts.AssertValid(nodeType);

                _txb.SetType(node, nodeType);
                _txb.SetInfo(node, new DottedNameInfo(node));
            }
        }
    }
}