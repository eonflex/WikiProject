import { useParams, Link, useNavigate } from 'react-router-dom';
import { useArticle } from '../hooks/useArticle';
import { LoadingSpinner, ErrorMessage } from '../components/StateDisplay';
import { formatDate, statusClass } from '../utils/format';
import { articleService } from '../services/articleService';
import { useState } from 'react';

export default function ArticleDetailPage() {
  const { id } = useParams<{ id: string }>();
  const numericId = Number(id);
  const { article, loading, error } = useArticle(numericId);
  const navigate = useNavigate();
  const [deleting, setDeleting] = useState(false);

  async function handleDelete() {
    if (!article) return;
    if (!window.confirm(`Delete "${article.title}"? This cannot be undone.`)) return;
    setDeleting(true);
    try {
      await articleService.delete(article.id);
      navigate('/articles');
    } catch {
      alert('Failed to delete article.');
      setDeleting(false);
    }
  }

  if (loading) return <LoadingSpinner />;
  if (error || !article) return <ErrorMessage message={error ?? 'Article not found.'} />;

  return (
    <div className="page">
      <div className="article-detail">
        <div className="article-detail-header">
          <div className="article-detail-meta">
            <span className="meta-category">📁 {article.category}</span>
            <span className={`status-badge ${statusClass(article.status)}`}>{article.status}</span>
          </div>

          <h1 className="article-detail-title">{article.title}</h1>
          <p className="article-detail-summary">{article.summary}</p>

          <div className="article-detail-dates">
            <span>Created {formatDate(article.createdAt)}</span>
            <span>·</span>
            <span>Updated {formatDate(article.updatedAt)}</span>
          </div>

          {article.tags.length > 0 && (
            <div className="meta-tags">
              {article.tags.map((tag) => (
                <span key={tag} className="tag">{tag}</span>
              ))}
            </div>
          )}

          <div className="article-detail-actions">
            <Link to={`/articles/${article.id}/edit`} className="btn btn-secondary">
              Edit
            </Link>
            <button
              className="btn btn-danger"
              onClick={handleDelete}
              disabled={deleting}
            >
              {deleting ? 'Deleting…' : 'Delete'}
            </button>
            <Link to="/articles" className="btn btn-ghost">
              ← Back to Articles
            </Link>
          </div>
        </div>

        <div className="article-content">
          {/* Render content as preformatted text; replace with a Markdown renderer if desired */}
          <pre className="content-body">{article.content}</pre>
        </div>
      </div>
    </div>
  );
}
