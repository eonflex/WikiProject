import type { ArticleStatus } from '../types';

interface Props {
  categories: string[];
  tags: string[];
  selectedCategory: string;
  selectedTag: string;
  selectedStatus: ArticleStatus | '';
  onCategoryChange: (v: string) => void;
  onTagChange: (v: string) => void;
  onStatusChange: (v: ArticleStatus | '') => void;
  onReset: () => void;
}

const STATUS_OPTIONS: Array<{ value: ArticleStatus | ''; label: string }> = [
  { value: '', label: 'All statuses' },
  { value: 'Published', label: 'Published' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Archived', label: 'Archived' },
];

export default function FilterControls({
  categories,
  tags,
  selectedCategory,
  selectedTag,
  selectedStatus,
  onCategoryChange,
  onTagChange,
  onStatusChange,
  onReset,
}: Props) {
  const hasFilters = selectedCategory || selectedTag || selectedStatus;

  return (
    <div className="filter-controls">
      <select
        className="filter-select"
        value={selectedCategory}
        onChange={(e) => onCategoryChange(e.target.value)}
        aria-label="Filter by category"
      >
        <option value="">All categories</option>
        {categories.map((c) => (
          <option key={c} value={c}>{c}</option>
        ))}
      </select>

      <select
        className="filter-select"
        value={selectedTag}
        onChange={(e) => onTagChange(e.target.value)}
        aria-label="Filter by tag"
      >
        <option value="">All tags</option>
        {tags.map((t) => (
          <option key={t} value={t}>{t}</option>
        ))}
      </select>

      <select
        className="filter-select"
        value={selectedStatus}
        onChange={(e) => onStatusChange(e.target.value as ArticleStatus | '')}
        aria-label="Filter by status"
      >
        {STATUS_OPTIONS.map(({ value, label }) => (
          <option key={value} value={value}>{label}</option>
        ))}
      </select>

      {hasFilters && (
        <button className="btn btn-ghost" onClick={onReset} type="button">
          Clear filters
        </button>
      )}
    </div>
  );
}
