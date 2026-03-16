import { useState, useEffect } from 'react';
import type { Article, CreateArticleRequest } from '../types';

interface Props {
  initial?: Article;
  onSubmit: (data: CreateArticleRequest) => Promise<void>;
  submitLabel?: string;
}

interface FormState {
  title: string;
  slug: string;
  summary: string;
  content: string;
  category: string;
  tags: string; // comma-separated in the UI
  status: number;
}

const STATUS_OPTIONS = [
  { value: 0, label: 'Draft' },
  { value: 1, label: 'Published' },
  { value: 2, label: 'Archived' },
];

function toStatusNumber(status: string): number {
  if (status === 'Published') return 1;
  if (status === 'Archived') return 2;
  return 0;
}

export default function ArticleForm({ initial, onSubmit, submitLabel = 'Save' }: Props) {
  const [form, setForm] = useState<FormState>({
    title: '',
    slug: '',
    summary: '',
    content: '',
    category: '',
    tags: '',
    status: 0,
  });
  const [errors, setErrors] = useState<Partial<Record<keyof FormState, string>>>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (initial) {
      setForm({
        title: initial.title,
        slug: initial.slug,
        summary: initial.summary,
        content: initial.content,
        category: initial.category,
        tags: initial.tags.join(', '),
        status: toStatusNumber(initial.status),
      });
    }
  }, [initial]);

  function set(field: keyof FormState) {
    return (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
      setForm((prev) => ({ ...prev, [field]: e.target.value }));
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    };
  }

  function validate(): boolean {
    const next: Partial<Record<keyof FormState, string>> = {};
    if (!form.title.trim()) next.title = 'Title is required.';
    if (!form.summary.trim()) next.summary = 'Summary is required.';
    if (!form.content.trim()) next.content = 'Content is required.';
    if (!form.category.trim()) next.category = 'Category is required.';
    if (form.slug && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.slug)) {
      next.slug = 'Slug must be lowercase alphanumeric with hyphens only.';
    }
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!validate()) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      const tags = form.tags
        .split(',')
        .map((t) => t.trim().toLowerCase())
        .filter(Boolean);

      await onSubmit({
        title: form.title.trim(),
        slug: form.slug.trim() || undefined,
        summary: form.summary.trim(),
        content: form.content.trim(),
        category: form.category.trim(),
        tags,
        status: Number(form.status),
      });
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : 'Failed to save article.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="article-form" noValidate>
      {submitError && (
        <div className="form-error-banner">{submitError}</div>
      )}

      <div className="form-group">
        <label className="form-label" htmlFor="title">Title *</label>
        <input
          id="title"
          className={`form-input${errors.title ? ' form-input--error' : ''}`}
          type="text"
          value={form.title}
          onChange={set('title')}
          placeholder="Article title"
          maxLength={200}
        />
        {errors.title && <span className="form-error">{errors.title}</span>}
      </div>

      <div className="form-group">
        <label className="form-label" htmlFor="slug">
          Slug <span className="form-hint">(auto-generated if empty)</span>
        </label>
        <input
          id="slug"
          className={`form-input${errors.slug ? ' form-input--error' : ''}`}
          type="text"
          value={form.slug}
          onChange={set('slug')}
          placeholder="my-article-slug"
          maxLength={200}
        />
        {errors.slug && <span className="form-error">{errors.slug}</span>}
      </div>

      <div className="form-group">
        <label className="form-label" htmlFor="summary">Summary *</label>
        <textarea
          id="summary"
          className={`form-input form-textarea${errors.summary ? ' form-input--error' : ''}`}
          value={form.summary}
          onChange={set('summary')}
          placeholder="Brief description of the article"
          rows={2}
          maxLength={500}
        />
        {errors.summary && <span className="form-error">{errors.summary}</span>}
      </div>

      <div className="form-group">
        <label className="form-label" htmlFor="content">Content *</label>
        <textarea
          id="content"
          className={`form-input form-textarea form-textarea--large${errors.content ? ' form-input--error' : ''}`}
          value={form.content}
          onChange={set('content')}
          placeholder="Article content (Markdown supported)"
          rows={16}
        />
        {errors.content && <span className="form-error">{errors.content}</span>}
      </div>

      <div className="form-row">
        <div className="form-group">
          <label className="form-label" htmlFor="category">Category *</label>
          <input
            id="category"
            className={`form-input${errors.category ? ' form-input--error' : ''}`}
            type="text"
            value={form.category}
            onChange={set('category')}
            placeholder="e.g. DevOps, Architecture"
            maxLength={100}
          />
          {errors.category && <span className="form-error">{errors.category}</span>}
        </div>

        <div className="form-group">
          <label className="form-label" htmlFor="status">Status</label>
          <select
            id="status"
            className="form-input form-select"
            value={form.status}
            onChange={set('status')}
          >
            {STATUS_OPTIONS.map(({ value, label }) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="form-group">
        <label className="form-label" htmlFor="tags">
          Tags <span className="form-hint">(comma-separated)</span>
        </label>
        <input
          id="tags"
          className="form-input"
          type="text"
          value={form.tags}
          onChange={set('tags')}
          placeholder="api, guide, database"
        />
      </div>

      <div className="form-actions">
        <button type="submit" className="btn btn-primary" disabled={submitting}>
          {submitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  );
}
