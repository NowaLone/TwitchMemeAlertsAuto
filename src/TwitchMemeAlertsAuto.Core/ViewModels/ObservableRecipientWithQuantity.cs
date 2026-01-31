using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public abstract partial class ObservableRecipientWithQuantity : ObservableRecipient, INotifyDataErrorInfo
	{
		private readonly Dictionary<string, List<string>> errors;

		[ObservableProperty]
		private string quantity;

		public ObservableRecipientWithQuantity()
		{
			this.errors = new Dictionary<string, List<string>>();
			Quantity = "0";
		}

		public bool HasErrors => errors.Any();

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		public IEnumerable GetErrors(string propertyName)
		{
			return errors.TryGetValue(propertyName, out var strings) ? strings : new List<string>();
		}

		private void AddError(string property, string error)
		{
			if (!errors.ContainsKey(property)) errors[property] = new List<string>();
			errors[property].Add(error);
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
		}

		private void ClearErrors(string property)
		{
			if (errors.Remove(property))
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
		}

		partial void OnQuantityChanged(string oldValue, string newValue)
		{
			ClearErrors(nameof(Quantity));

			if (!int.TryParse(newValue, out var result) || result <= 0 || result > 1000)
			{
				AddError(nameof(Quantity), "Quantity must be a positive integer and less then 1000.");
			}
		}
	}
}