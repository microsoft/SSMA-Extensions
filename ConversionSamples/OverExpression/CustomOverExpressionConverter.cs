// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CustomOverExpressionConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.SSMA.Framework.Generic.Converters;
using Microsoft.SSMA.Framework.Generic.Converters.Default;
using Microsoft.SSMA.Framework.Generic.XTree;
using Microsoft.SSMA.Framework.Oracle.Constants;
using Microsoft.SSMA.Framework.Oracle.Generic;
using Microsoft.SSMA.Framework.Oracle.SqlServerConverter.NodeConverters;
using Microsoft.SSMA.Framework.SqlServer.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace SsmaExtensions.ConversionSamples.OverExpression
{
    /// <summary>
    /// Custom converter for the over-expression node.
    /// </summary>
    [Export(typeof(INodeConverter))]
    [NodeConverter(OracleParserConstants.NodeNames.OVER_EXPRESSION)]
    public class CustomOverExpressionConverter : O2SSNodeConverter
    {
        /// <summary>
        /// Set of all aggregation functions supported by this converter.
        /// </summary>
        private static readonly HashSet<string> SupportedAggregationFunctions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FIRST_VALUE"
            };

        /// <summary>
        /// Original converter for over-expression node.
        /// </summary>
        private readonly INodeConverter originalOverExpressionConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomOverExpressionConverter"/> class
        /// with the specified owning document converter.
        /// </summary>
        /// <param name="documentConverter">Owning document converter instance</param>
        [ImportingConstructor]
        public CustomOverExpressionConverter(DefaultDocumentConverter documentConverter)
            : base(documentConverter)
        {
            originalOverExpressionConverter = new OverExpressionConverter(documentConverter);
        }

        /// <summary>
        /// Converts the <paramref name="sourceNode"/> and attaches conversion results to the <paramref name="targetParent"/>.
        /// </summary>
        /// <param name="sourceNode">Source node to convert</param>
        /// <param name="targetParent">Target parent node</param>
        /// <param name="context">Current conversion context</param>
        /// <returns>Converted node</returns>
        public override XNode ConvertNode(XNode sourceNode, XNode targetParent, IConversionContext context)
        {
            // Typical over-expression node will have two children: aggregate function identifier and over-expression parameters.
            if (sourceNode.Children.Count == 2)
            {
                // Get the function identifier node
                var sourceFunctionIdentifierNode = sourceNode.Children[0];

                // Extract function name
                var sourceFunctionCompoundName =
                    OracleNameProcessor.Instance.GetNameFromIdentifier(sourceFunctionIdentifierNode);

                // Check, if function identifier is a simple one-part identifier and is suitable for our conversion
                if (sourceFunctionCompoundName.Count == 1
                    && SupportedAggregationFunctions.Contains(sourceFunctionCompoundName.Last.NormalName))
                {
                    // Create over-expression node in the target tree
                    var targetOverExpressionNode = targetParent.Children.AddNew(SqlServerParserConstants.NodeNames.OVER_EXPRESSION);

                    // Create identifier node for the aggregate function in the target over-expression
                    var targetFunctionIdentifierNode = targetOverExpressionNode.Children.AddNew(SqlServerParserConstants.NodeNames.SIMPLE_IDENTIFIER);

                    targetFunctionIdentifierNode.Attributes[SqlServerParserConstants.AttributeNames.TEXT] =
                        targetFunctionIdentifierNode.Attributes[SqlServerParserConstants.AttributeNames.VALUE] =
                            sourceFunctionCompoundName.Last.NormalName;

                    // Create parameters node under target function identifier node
                    var targetFunctionIdentifierParamsNode =
                        targetFunctionIdentifierNode.Children.AddNew(SqlServerParserConstants.NodeNames.SIMPLE_IDENTIFIER_PARAMS);

                    // Convert function arguments
                    base.ConvertNode(
                        sourceFunctionIdentifierNode.GetAllNodesByName(SqlServerParserConstants.NodeNames.EXPRESSION_LIST).First(),
                        targetFunctionIdentifierParamsNode,
                        context);

                    // Get the parameters for over expression
                    var sourceOverExpressionParametersNode = sourceNode.Children[1];

                    // Create parameters node in target over-expression
                    var overExpressionParams = targetOverExpressionNode.Children.AddNew(SqlServerParserConstants.NodeNames.OVER_EXPRESSION_PARAMS);

                    // Convert first child of source over-expression-parameters
                    base.ConvertNode(
                        sourceOverExpressionParametersNode.FirstChild,
                        overExpressionParams,
                        context);

                    return targetOverExpressionNode;
                }
            }

            // Fallback to original over-expression node converter, in case we don't support the input
            return originalOverExpressionConverter.ConvertNode(sourceNode, targetParent, context);
        }
    }
}
