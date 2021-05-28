namespace BlazorFluentUI
{
    public class MyBFUNumericTextField<TValue> : MyBFUTextFieldBase<TValue>
    {
        public MyBFUNumericTextField()
        {
            InputType = InputType.Number;
            AutoComplete = AutoComplete.Off;
        }
    }
}
