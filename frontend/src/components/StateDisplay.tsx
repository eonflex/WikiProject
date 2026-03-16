interface Props {
  message?: string;
}

export function LoadingSpinner({ message = 'Loading…' }: Props) {
  return (
    <div className="state-container">
      <div className="spinner" aria-label="Loading" />
      <p className="state-message">{message}</p>
    </div>
  );
}

export function ErrorMessage({ message = 'Something went wrong.' }: Props) {
  return (
    <div className="state-container state-error">
      <span className="state-icon">⚠️</span>
      <p className="state-message">{message}</p>
    </div>
  );
}

export function EmptyState({ message = 'No articles found.' }: Props) {
  return (
    <div className="state-container state-empty">
      <span className="state-icon">📭</span>
      <p className="state-message">{message}</p>
    </div>
  );
}
