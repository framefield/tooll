// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* 
 * Vorschlag fuer einen operator namespace. Das mit den schnittstellen muesste man
 * nicht sofort machen, aber langfristig sollte das ziel sein, dass man mit schnittstellen
 * arbeitet die einem bestimmte informationen aber nur lesend zur verfuegung stellen. 
 * Modifizieren tut man objekte im core dann ueber ein kommando und den entsprechenden IDs
 * die natuerlich ueber die schnittstellen abgefragt werden koennen. Das sorft dafuer das
 * man nicht das problem hat das direkte modifikationen an objekten moeglicherweise die
 * konsistenz auch bzgl persistenz ausm tritt bringen. Des weiteren macht man sich auch 
 * sinnvolle gedanken was letztendlich ueberhaupt oeffentlich sein soll und was nicht.
 */
namespace Framefield.Core.Operators
{
    public interface IOperatorInstance { }
    internal class Instance : IOperatorInstance { } // Operator

    public interface IInstanceConnection { }
    internal class InstanceConnection : IInstanceConnection { }  // Connection

    public interface IOperatorDefinition { }
    internal class Defintion : IOperatorDefinition { } // MetaOperator

    internal class ConnectionDefinition { } // MetaConnection

    public interface IInputDefinition { }
    internal class InputDefinition : IInputDefinition { } // MetaInput

    public interface IOutputDefinition { }
    class OutputDefinition : IOutputDefinition { } // MetaOutput

    public interface IOperatorElementDefinition { }
    class ElementDefinition : IOperatorElementDefinition { } // MetaOperatorPart

    public class OperatorElementFunction { } // MetaOperatorPart.Function

    public interface IOperatorElementIntance { }
    internal class ElementInstance : IOperatorElementIntance { } // OperatorPart

    public class OperatorElementTraits { } // OperatorPartCharacteristics

    public class EvaluationContext { } // OperatorPartContext
}
