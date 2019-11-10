﻿using System;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSSArt.StateMachine.Test
{
	[TestClass]
	public class ScxmlToDomTest
	{
		private IStateMachine GetStateMachine(string scxml)
		{
			using (var textReader = new StringReader(scxml))
			using (var reader = XmlReader.Create(textReader))
			{
				return new ScxmlDirector(reader, new BuilderFactory()).ConstructStateMachine();
			}
		}

		private IStateMachine GetStateMachineWithRoot(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0'>" + xml + "</scxml>");

		private IStateMachine GetStateMachineXyzDataModel(string xml) => GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='xyz'>" + xml + "</scxml>");

		[TestMethod]
		public void RootElementEmptyTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0'/>");
			Assert.IsNull(sm.DataModelType);
			Assert.AreEqual(BindingType.Early, sm.Binding);
			Assert.IsNull(sm.DataModel);
			Assert.IsNull(sm.Initial);
			Assert.IsNull(sm.Name);
			Assert.IsNull(sm.Script);
			Assert.IsNull(sm.States);
		}

		[TestMethod]
		public void RootElementBeginEndTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0'></scxml>");
			Assert.IsNull(sm.DataModel);
			Assert.IsNull(sm.Initial);
			Assert.IsNull(sm.Name);
			Assert.IsNull(sm.Script);
			Assert.IsNull(sm.States);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementNameFailTest()
		{
			GetStateMachine("<no-scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0'/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementVersionFailTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='0.2'/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementUnknownAttributesTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' attr0='00' attr0='11' attr1='22' />");
		}

		[TestMethod]
		public void RootElementDataModelTypeTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0'/>");
			Assert.IsNull(sm.DataModelType);

			var sm1 = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='none'/>");
			Assert.AreEqual(expected: "none", sm1.DataModelType);

			var sm2 = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' datamodel='ecmascript'/>");
			Assert.AreEqual(expected: "ecmascript", sm2.DataModelType);
		}

		[TestMethod]
		public void RootElementBindingTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' binding='early'/>");
			Assert.AreEqual(BindingType.Early, sm.Binding);

			var sm2 = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' binding='late'/>");
			Assert.AreEqual(BindingType.Late, sm2.Binding);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementInvalidEmptyBindingTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' binding=''/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementInvalidWrongNameBindingTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' binding='invalid-binding'/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementInvalidUpperCaseBindingTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' binding='Late'/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementEmptyNameFailTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' name=''/>");
			Assert.AreEqual(expected: "", sm.Name);
		}

		[TestMethod]
		public void RootElementNameTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' name='It is name'/>");
			Assert.AreEqual(expected: "It is name", sm.Name);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementEmptyInitialTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' initial=''/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void RootElementSpaceInitialTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' initial=' '/>");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void RootElementInitialFailTest()
		{
			GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' initial=' trg2  trg1 '/>");
		}

		[TestMethod]
		public void RootElementInitialTest()
		{
			var sm = GetStateMachine("<scxml xmlns='http://www.w3.org/2005/07/scxml' version='1.0' initial=' trg2  trg1 '><state/></scxml>");
			Assert.AreEqual((Identifier) "trg2", sm.Initial.Transition.Target[0]);
			Assert.AreEqual((Identifier) "trg1", sm.Initial.Transition.Target[1]);
			Assert.AreEqual(expected: 2, sm.Initial.Transition.Target.Count);
		}

		[TestMethod]
		public void DataModelTest()
		{
			var sm = GetStateMachineXyzDataModel("<datamodel></datamodel>");
			Assert.IsNotNull(sm.DataModel);
			Assert.IsNull(sm.DataModel.Data);
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void IncorrectXmlTest()
		{
			GetStateMachineWithRoot("<datamodel><data id='a'/><data id='b'></data><data id='c' src='c-src/><data id='d' expr='d-expr'/><data id='e'>e-body</data></datamodel>");
		}

		[TestMethod]
		public void DataModelWithDataTest()
		{
			var sm = GetStateMachineXyzDataModel("<datamodel><data id='a'/><data id='b'></data><data id='c' src='c-src'/><data id='d' expr='d-expr'/><data id='e'>e-body</data></datamodel>");
			Assert.IsNotNull(sm.DataModel);
			Assert.AreEqual(expected: 5, sm.DataModel.Data.Count);

			Assert.AreEqual(expected: "a", sm.DataModel.Data[0].Id);
			Assert.IsNull(sm.DataModel.Data[0].Source);
			Assert.IsNull(sm.DataModel.Data[0].Expression);

			Assert.AreEqual(expected: "b", sm.DataModel.Data[1].Id);
			Assert.IsNull(sm.DataModel.Data[1].Source);
			Assert.AreEqual(expected: "", sm.DataModel.Data[1].InlineContent);

			Assert.AreEqual(expected: "c", sm.DataModel.Data[2].Id);
			Assert.AreEqual(expected: "c-src", sm.DataModel.Data[2].Source.Uri.ToString());
			Assert.IsNull(sm.DataModel.Data[2].Expression);

			Assert.AreEqual(expected: "d", sm.DataModel.Data[3].Id);
			Assert.IsNull(sm.DataModel.Data[3].Source);
			Assert.AreEqual(expected: "d-expr", sm.DataModel.Data[3].Expression.Expression);

			Assert.AreEqual(expected: "e", sm.DataModel.Data[4].Id);
			Assert.IsNull(sm.DataModel.Data[4].Source);
			Assert.AreEqual(expected: "e-body", sm.DataModel.Data[4].InlineContent);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void TwoDataModelTest()
		{
			GetStateMachineXyzDataModel("<datamodel/><datamodel/>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void DataNoIdTest()
		{
			GetStateMachineXyzDataModel("<datamodel><data></data></datamodel>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void DataSrcAndExprFailTest()
		{
			GetStateMachineXyzDataModel("<datamodel><data id='a' src='domain' expr='some-expr'/></datamodel>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void DataSrcAndBodyFailTest()
		{
			GetStateMachineXyzDataModel("<datamodel><data id='a' src='domain'>123</data></datamodel>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void DataBodyAndExprFailTest()
		{
			GetStateMachineXyzDataModel("<datamodel><data id='a' expr='some-expr'>123</data></datamodel>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void DataSrcAndBodyAndExprFailTest()
		{
			GetStateMachineXyzDataModel("<datamodel><data id='a' src='s-src' expr='some-expr'>123</data></datamodel>");
		}

		[TestMethod]
		public void GlobalScriptTest()
		{
			var sm = GetStateMachineXyzDataModel("<script/>");
			Assert.IsInstanceOfType(sm.Script, typeof(IScript));
			var script = (IScript) sm.Script;
			Assert.IsNull(script.Content);
			Assert.IsNull(script.Source);
		}

		[TestMethod]
		public void GlobalScriptBodyTest()
		{
			var sm = GetStateMachineXyzDataModel("<script><any_script xmlns='aaa'>345</any_script></script>");
			Assert.IsInstanceOfType(sm.Script, typeof(IScript));
			var script = (IScript) sm.Script;
			Assert.IsInstanceOfType(script.Content, typeof(IScriptExpression));
			var scriptExpression = script.Content;
			Assert.AreEqual(expected: "<any_script xmlns=\"aaa\">345</any_script>", scriptExpression.Expression);
			Assert.IsNull(script.Source);
		}

		[TestMethod]
		public void GlobalScriptSrcTest()
		{
			var sm = GetStateMachineXyzDataModel("<script src='s-src'/>");
			Assert.IsInstanceOfType(sm.Script, typeof(IScript));
			var script = (IScript) sm.Script;
			Assert.IsInstanceOfType(script.Source, typeof(IExternalScriptExpression));
			var externalScriptExpression = script.Source;
			Assert.AreEqual(expected: "s-src", externalScriptExpression.Uri.ToString());
			Assert.IsNull(script.Content);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void GlobalScriptSrcAndBodyFailTest()
		{
			GetStateMachineXyzDataModel("<script src='s-src'>body</script>");
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void MultipleGlobalScriptFailTest()
		{
			GetStateMachineXyzDataModel("<script/><script/>");
		}

		[TestMethod]
		[DataRow("state")]
		[DataRow("parallel")]
		[DataRow("final")]
		public void MultipleStateTest(string element)
		{
			GetStateMachineWithRoot($"<{element}/>");
			GetStateMachineWithRoot($"<{element}></{element}>");
			GetStateMachineWithRoot($"<{element}/><{element}/>");
			GetStateMachineWithRoot($"<{element}/><{element}/><{element}/>");
		}

		[TestMethod]
		public void StateNoAttrTest()
		{
			var sm = GetStateMachineWithRoot("<state/>");
			Assert.IsNull(((IState) sm.States[0]).Id);
			Assert.IsNull(((IState) sm.States[0]).Initial);
		}

		[TestMethod]
		public void StateIdTest()
		{
			var sm = GetStateMachineWithRoot("<state id='a'/>");
			Assert.AreEqual((Identifier) "a", ((IState) sm.States[0]).Id);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void StateIdFailTest()
		{
			var sm = GetStateMachineWithRoot("<state id='a b'/>");
			Assert.AreEqual((Identifier) "a", ((IState) sm.States[0]).Id);
		}

		[TestMethod]
		[ExpectedException(typeof(XmlException))]
		public void StateInitialFailForAtomicStateTest()
		{
			GetStateMachineWithRoot("<state initial='id id2'/>");
		}

		[TestMethod]
		public void StateInitialTest()
		{
			var sm = GetStateMachineWithRoot("<state initial='id id2'><parallel/></state>");
			Assert.IsNull(((IState) sm.States[0]).Id);
			Assert.AreEqual((Identifier) "id", ((IState) sm.States[0]).Initial.Transition.Target[0]);
			Assert.AreEqual((Identifier) "id2", ((IState) sm.States[0]).Initial.Transition.Target[1]);
		}

		[TestMethod]
		public void ParallelNoIdTest()
		{
			var sm = GetStateMachineWithRoot("<parallel/>");
			Assert.IsNull(((IParallel) sm.States[0]).Id);
		}

		[TestMethod]
		public void ParallelIdTest()
		{
			var sm = GetStateMachineWithRoot("<parallel id='a'/>");
			Assert.AreEqual((Identifier) "a", ((IParallel) sm.States[0]).Id);
		}

		[TestMethod]
		public void FinalNoIdTest()
		{
			var sm = GetStateMachineWithRoot("<final/>");
			Assert.IsNull(((IFinal) sm.States[0]).Id);
		}

		[TestMethod]
		public void FinalIdTest()
		{
			var sm = GetStateMachineWithRoot("<final id='a'/>");
			Assert.AreEqual((Identifier) "a", ((IFinal) sm.States[0]).Id);
		}

		[TestMethod]
		[DataRow("unknown")]
		[DataRow("initial")]
		[DataRow("history")]
		[DataRow("onentry")]
		[DataRow("onexit")]
		[DataRow("invoke")]
		[DataRow("transition")]
		[ExpectedException(typeof(XmlException))]
		public void UnknownElementTest(string element)
		{
			GetStateMachineWithRoot($"<{element}/>");
		}

		[TestMethod]
		public void AtomicStateTeat()
		{
			var sm = GetStateMachineWithRoot("<state><onentry/><onexit/><transition event='e'/><invoke/></state>");

			var state = (IState) sm.States[0];
			Assert.IsNull(state.Id);
			Assert.AreEqual(expected: 1, state.OnEntry.Count);
			Assert.AreEqual(expected: 1, state.OnExit.Count);
			Assert.AreEqual(expected: 1, state.Transitions.Count);
			Assert.AreEqual(expected: 1, state.Invoke.Count);
			Assert.IsNull(state.DataModel);
			Assert.IsNull(state.HistoryStates);
			Assert.IsNull(state.Initial);
			Assert.IsNull(state.States);
		}
	}
}