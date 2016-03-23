// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using Newtonsoft.Json;
using System;

namespace Framefield.Core
{
    public class MetaConnection
    {
        [JsonProperty]
        public Guid ID { get; internal set; }
        public Guid SourceOpID { get; set; }
        public Guid SourceOpPartID { get; set; }
        public Guid TargetOpID { get; set; }
        public Guid TargetOpPartID { get; set; }

        public MetaConnection() {} // for json deserialization
        public MetaConnection(Guid sourceOpID, Guid sourceOpPartID, Guid targetOpID, Guid targetOpPartID) :
            this(Guid.NewGuid(), sourceOpID, sourceOpPartID, targetOpID, targetOpPartID) 
        {
        }

        internal MetaConnection(Guid id, Guid sourceOpID, Guid sourceOpPartID, Guid targetOpID, Guid targetOpPartID) 
        {
            ID = id;
            SourceOpID = sourceOpID;
            SourceOpPartID = sourceOpPartID;
            TargetOpID = targetOpID;
            TargetOpPartID = targetOpPartID;
        }

        public MetaConnection(Connection con)
            : this(con.ID,
                   (con.SourceOp == null) ? Guid.Empty : con.SourceOp.ID, con.SourceOpPart.ID,
                   (con.TargetOp == null) ? Guid.Empty : con.TargetOp.ID, con.TargetOpPart.ID)
        {
        }
    }
}
