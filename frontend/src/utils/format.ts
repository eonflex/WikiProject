/** Formats an ISO date string to a readable local date. */
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

/** Returns a CSS class name for the given article status. */
export function statusClass(status: string): string {
  switch (status) {
    case 'Published': return 'status-published';
    case 'Draft': return 'status-draft';
    case 'Archived': return 'status-archived';
    default: return '';
  }
}
