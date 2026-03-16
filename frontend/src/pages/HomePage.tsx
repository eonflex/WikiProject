import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { articleService } from '../services/articleService';
import type { ArticleSummary } from '../types';
import ArticleCard from '../components/ArticleCard';
import { LoadingSpinner } from '../components/StateDisplay';

export default function HomePage() {
  const [recent, setRecent] = useState<ArticleSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    articleService
      .list({ status: 'Published', pageSize: 6 })
      .then((res) => setRecent(res.items))
      .catch(() => setRecent([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="page">
      <section className="hero">
        <h1 className="hero-title">WikiProject</h1>
        <p className="hero-subtitle">
          Your team's internal knowledge base — find answers, share knowledge, build clarity.
        </p>
        <div className="hero-actions">
          <Link to="/articles" className="btn btn-primary">Browse Articles</Link>
          <Link to="/articles/new" className="btn btn-secondary">New Article</Link>
        </div>
      </section>

      <section className="home-section">
        <div className="section-header">
          <h2>Recently Updated</h2>
          <Link to="/articles" className="section-link">View all →</Link>
        </div>

        {loading ? (
          <LoadingSpinner />
        ) : recent.length === 0 ? (
          <p className="empty-hint">No published articles yet. <Link to="/articles/new">Create the first one!</Link></p>
        ) : (
          <div className="article-grid">
            {recent.map((a) => (
              <ArticleCard key={a.id} article={a} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
