import { Link } from 'react-router-dom';
import type { ArticleSummary } from '../types';
import { formatDate, statusClass } from '../utils/format';

interface Props {
  article: ArticleSummary;
}

export default function ArticleCard({ article }: Props) {
  return (
    <div className="article-card">
      <div className="article-card-header">
        <Link to={`/articles/${article.id}`} className="article-card-title">
          {article.title}
        </Link>
        <span className={`status-badge ${statusClass(article.status)}`}>
          {article.status}
        </span>
      </div>

      <p className="article-card-summary">{article.summary}</p>

      <div className="article-card-meta">
        <span className="meta-category">📁 {article.category}</span>
        {article.tags.length > 0 && (
          <div className="meta-tags">
            {article.tags.map((tag) => (
              <span key={tag} className="tag">{tag}</span>
            ))}
          </div>
        )}
        <span className="meta-date">Updated {formatDate(article.updatedAt)}</span>
      </div>
    </div>
  );
}
