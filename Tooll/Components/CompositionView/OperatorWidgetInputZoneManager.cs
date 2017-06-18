// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framefield.Core;
using System.Windows;

namespace Framefield.Tooll.Components.CompositionView
{
    /*
     * Converts the relative position of a Connection-DropOver-Event above
     * an Operator into a meaningful representation that can be used to
     * highlight the UI and indicate possible drop zones:
     *
     * Is uses the connection and the Operator to compute:
     * - A list of input zones
     * - The currently active input zone
     * - The type and of the currently active input zone
     * - An indication if the "select parameter from list"-zone is active
     */

    public class OperatorWidgetInputZone
    {
        public Core.OperatorPart Input;
        public MetaInput MetaInput;

        public double LeftPosition;
        public double Width = 1;
        public bool IsBelowMouse;
        public int MultiInputIndex;
        public OperatorPart ConnectedToOutput;
        public bool InsertAtMultiInputIndex;
    }

    public static class OperatorWidgetInputZoneManager
    {
        static public List<OperatorWidgetInputZone> ComputeInputZonesForOp(IConnectionLineTarget op)
        {
            var opWidget = op as OperatorWidget;
            if (opWidget != null)
                return ComputeInputZonesForOperatorWidget(opWidget);

            var outputWidget = op as OutputWidget;
            if (outputWidget != null)
                return ComputeIntputZonesForOutputWidget(outputWidget);

            throw new Exception("Can't compute input zones for unknown widget type");
        }

        private static List<OperatorWidgetInputZone> ComputeIntputZonesForOutputWidget(OutputWidget outputWidget)
        {
            var zones = new List<OperatorWidgetInputZone>();

            zones.Add(new OperatorWidgetInputZone()
            {
                Input = outputWidget.OperatorPart,
                MetaInput = null,
                Width = outputWidget.Width,
                InsertAtMultiInputIndex = true
            });

            return zones;
        }

        private static List<OperatorWidgetInputZone> ComputeInputZonesForOperatorWidget(OperatorWidget opWidget)
        {
            var zones = new List<OperatorWidgetInputZone>();

            // First collect inputs that are relevant or connected
            var relevantOrConnectedInputs = new List<OperatorPart>();
            foreach (var input in opWidget.Inputs)
            {
                var metaInput = input.Parent.GetMetaInput(input);
                if (metaInput == null)
                    throw new Exception("Invalid OperatorPart references in InputZone");

                if (metaInput.Relevance == MetaInput.RelevanceType.Required
                    || metaInput.Relevance == MetaInput.RelevanceType.Relevant)
                {
                    relevantOrConnectedInputs.Add(input);
                }
                else
                {
                    if (input.Connections.Count() > 0)
                    {
                        var animationConnection = Animation.GetRegardingAnimationOpPart(input.Connections[0]);
                        if (animationConnection == null)
                        {
                            // Add non-animated connections
                            relevantOrConnectedInputs.Add(input);
                        }
                    }
                }
            }

            const double WIDTH_OF_MULTIINPUT_ZONES = 1.0 / 3.0;

            /* Roll out zones multi-inputs and the slots for prepending
             * a connection at the first field or inserting connections
             * between existing connections.
             *
             */
            foreach (var input in relevantOrConnectedInputs)
            {
                var metaInput = input.Parent.GetMetaInput(input);
                if (metaInput.IsMultiInput)
                {
                    if (!input.Connections.Any())
                    {
                        // empty multi-input
                        zones.Add(new OperatorWidgetInputZone()
                        {
                            Input = input,
                            MetaInput = metaInput,
                            InsertAtMultiInputIndex = true,
                        });
                    }
                    else
                    {
                        zones.Add(new OperatorWidgetInputZone()
                        {
                            Input = input,
                            MetaInput = metaInput,
                            InsertAtMultiInputIndex = true,
                            Width = WIDTH_OF_MULTIINPUT_ZONES,
                            MultiInputIndex = 0,
                        });

                        for (var multiInputIndex = 0; multiInputIndex < input.Connections.Count; ++multiInputIndex)
                        {
                            var connectedTo = input.Connections[multiInputIndex];

                            // multi-input connection
                            zones.Add(new OperatorWidgetInputZone()
                            {
                                Input = input,
                                MetaInput = metaInput,
                                Width = WIDTH_OF_MULTIINPUT_ZONES,
                                MultiInputIndex = multiInputIndex,
                            });
                            zones.Add(new OperatorWidgetInputZone()
                            {
                                Input = input,
                                MetaInput = metaInput,
                                Width = WIDTH_OF_MULTIINPUT_ZONES,
                                MultiInputIndex = multiInputIndex + 1,
                                InsertAtMultiInputIndex = true
                            });
                        }
                    }
                }
                else
                {
                    // Normal input
                    zones.Add(new OperatorWidgetInputZone()
                    {
                        Input = input,
                        MetaInput = metaInput
                    });
                }
            }

            // Now distibute the width to the width of the operator
            double widthSum = 0;
            foreach (var zone in zones)
            {
                widthSum += zone.Width;
            }

            double posX = 0;
            for (var i = 0; i < zones.Count; ++i)
            {
                var widthInsideOp = zones[i].Width / widthSum * opWidget.Width;
                zones[i].Width = widthInsideOp - 1; // requires zones to be a class
                zones[i].LeftPosition = posX;
                posX += widthInsideOp;
            }

            return zones;
        }

        public static OperatorWidgetInputZone FindZoneBelowMouse(List<OperatorWidgetInputZone> zones, Point mousePosition)
        {
            foreach (var zone in zones)
            {
                if (zone.LeftPosition <= mousePosition.X && zone.LeftPosition + zone.Width > mousePosition.X
                    && mousePosition.Y > 0 && mousePosition.Y < 25)
                {
                    return zone;
                }
            }
            return null;
        }
    }
}