﻿using LoxInterpreter.Exceptions;
using LoxInterpreter.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxInterpreter.Parsing
{
	public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
	{
		private Environment environment = new();
		public void Interpret(List<Stmt> statements)
		{
			try
			{
				foreach (var statement in statements)
				{
					this.Execute(statement);
				}
			}
			catch (RuntimeError e)
			{
				Lox.RuntimeError(e);
			}
		}

		private void Execute(Stmt stmt)
		{
			stmt.Accept(this);
		}

		private void ExecuteBlock(List<Stmt> statements, Environment environment)
		{
			var previous = this.environment;
			try
			{
				this.environment = environment;

				foreach (var statement in statements)
				{
					this.Execute(statement);
				}
			}
			finally
			{
				this.environment = previous;
			}
		}

		#region Interfaces
		public object VisitBlockStmt(Stmt.Block stmt)
		{
			this.ExecuteBlock(stmt.statements, new Environment(environment));
			return null;
		}

		public object VisitBinaryExpr(Expr.Binary expr)
		{
			var left = this.Evaluate(expr.left);
			var right = this.Evaluate(expr.right);

			switch (expr.oper.Type)
			{
				case TokenType.GREATER:
					this.CheckNumberOperands(expr.oper, left, right);
					return (double)left > (double)right;
				case TokenType.GREATER_EQUAL:
					this.CheckNumberOperands(expr.oper, left, right);
					return (double)left >= (double)right;
				case TokenType.LESS:
					this.CheckNumberOperands(expr.oper, left, right);
					return (double)left < (double)right;
				case TokenType.LESS_EQUAL:
					this.CheckNumberOperands(expr.oper, left, right);
					return (double)left <= (double)right;

				case TokenType.EQUAL_EQUAL:
					return this.IsEqual(left, right);
				case TokenType.BANG_EQUAL:
					return !this.IsEqual(left, right);

				case TokenType.MINUS:
					this.CheckNumberOperands(expr.oper, left, right);
					return (double)left - (double)right;
				case TokenType.SLASH:
					this.CheckNumberOperands(expr.oper, left, right);
					if ((double)right == 0) throw new RuntimeError(expr.oper, "Cannot divide by zero");
					return (double)left / (double)right;
				case TokenType.ASTERISK:
					this.CheckNumberOperands(expr.oper, left, right);
					return (double)left * (double)right;

				case TokenType.PLUS:
					if (left is double lhsD && right is double rhsD)
					{
						return lhsD + rhsD;
					}
					if (left is string lhsS && right is string rhsS)
					{
						return lhsS + rhsS;
					}
					throw new RuntimeError(expr.oper, "Operands must be two numbers OR two strings.");
			}
			return null;
		}

		public object VisitGroupingExpr(Expr.Grouping expr)
		{
			return this.Evaluate(expr.expression);
		}

		public object VisitLiteralExpr(Expr.Literal expr)
		{
			return expr.value;
		}

		public object VisitLogicalExpr(Expr.Logical expr)
		{
			var left = this.Evaluate(expr.left);

			if (expr.oper.Type == TokenType.OR)
			{
				if (this.IsTruthy(left)) return left;
			}
			else
			{
				if (!this.IsTruthy(left)) return left;
			}

			return this.Evaluate(expr.right);
		}

		public object VisitUnaryExpr(Expr.Unary expr)
		{
			//Evaluate the expression value first
			var right = this.Evaluate(expr.right);

			switch (expr.oper.Type)
			{
				case TokenType.BANG:
					return !this.IsTruthy(right);
				case TokenType.MINUS:
					this.CheckNumberOperand(expr.oper, right);
					return -(double)right;
			}
			return null;
		}

		public object VisitVariableExpr(Expr.Variable expr)
		{
			return environment.Get(expr.name);
		}

		public object VisitExpressionStmt(Stmt.Expression stmt)
		{
			_ = this.Evaluate(stmt.expression);
			return null;
		}

		public object VisitIfStmt(Stmt.If stmt)
		{
			if (this.IsTruthy(this.Evaluate(stmt.condition)))
			{
				this.Execute(stmt.thenBranch);
			}
			else if (stmt.elseBranch != null)
			{
				this.Execute(stmt.elseBranch);
			}
			return null;
		}

		public object VisitPrintStmt(Stmt.Print stmt)
		{
			var value = this.Evaluate(stmt.expression);
			Console.WriteLine(this.Stringify(value));
			return null;
		}

		public object VisitVarStmt(Stmt.Var stmt)
		{
			object value = null;
			if (stmt.initializer != null)
			{
				value = this.Evaluate(stmt.initializer);
			}
			this.environment.Define(stmt.name.Lexeme, value);
			return null;
		}

		public object VisitWhileStmt(Stmt.While stmt)
		{
			while (this.IsTruthy(this.Evaluate(stmt.condition)))
			{
				this.Execute(stmt.body);
			}
			return null;
		}

		public object VisitAssignExpr(Expr.Assign expr)
		{
			var value = this.Evaluate(expr.value);
			this.environment.Assign(expr.name, value);
			return value;
		}
		#endregion

		/// <summary>
		/// Recursively send the value back through the visitor
		/// </summary>
		/// <param name="expr"></param>
		/// <returns></returns>
		private object Evaluate(Expr expr)
		{
			return expr.Accept(this);
		}

		private bool IsTruthy(object obj)
		{
			if (obj == null) return false;
			if (obj is bool boolObj) return boolObj;
			return true;
		}

		private bool IsEqual(object a, object b)
		{
			if (a == null && b == null) return true;
			if (a == null) return false;
			return a.Equals(b);
		}

		private string Stringify(object obj)
		{
			if (obj == null) return "nil";
			var text = obj.ToString();
			if (obj is double)
			{
				if (text.EndsWith(".0"))
				{
					text = text[0..^2];
				}
			}
			return text;
		}

		private void CheckNumberOperand(Token oper, object operand)
		{
			if (operand is double) return;
			throw new RuntimeError(oper, "Operand must be a number.");
		}

		private void CheckNumberOperands(Token oper, object left, object right)
		{
			if (left is double && right is double) return;
			throw new RuntimeError(oper, "Operands must be numbers.");
		}

	}
}
