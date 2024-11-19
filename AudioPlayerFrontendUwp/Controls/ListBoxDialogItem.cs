namespace AudioPlayerFrontend.Controls
{
    class ListBoxDialogItem<T>
    {
        public T Value { get; }

        public object Content { get; }

        public ListBoxDialogItem(T value, object content)
        {
            Value = value;
            Content = content;
        }
    }
}
