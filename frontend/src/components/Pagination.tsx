interface Props {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({ page, totalPages, totalCount, pageSize, onPageChange }: Props) {
  if (totalPages <= 1) return null;

  const start = (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalCount);

  return (
    <div className="pagination">
      <span className="pagination-info">
        {start}–{end} of {totalCount}
      </span>
      <div className="pagination-controls">
        <button
          className="btn btn-ghost"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          ← Prev
        </button>
        <span className="pagination-page">{page} / {totalPages}</span>
        <button
          className="btn btn-ghost"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          Next →
        </button>
      </div>
    </div>
  );
}
