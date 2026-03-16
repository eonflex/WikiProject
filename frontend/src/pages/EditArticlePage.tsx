import { useParams, useNavigate } from 'react-router-dom';
import { useArticle } from '../hooks/useArticle';
import ArticleForm from '../components/ArticleForm';
import { LoadingSpinner, ErrorMessage } from '../components/StateDisplay';
import { articleService } from '../services/articleService';
import type { CreateArticleRequest } from '../types';

export default function EditArticlePage() {
  const { id } = useParams<{ id: string }>();
  const numericId = Number(id);
  const { article, loading, error } = useArticle(numericId);
  const navigate = useNavigate();

  async function handleSubmit(data: CreateArticleRequest) {
    await articleService.update(numericId, data);
    navigate(`/articles/${numericId}`);
  }

  if (loading) return <LoadingSpinner />;
  if (error || !article) return <ErrorMessage message={error ?? 'Article not found.'} />;

  return (
    <div className="page">
      <div className="page-header">
        <h1>Edit: {article.title}</h1>
      </div>
      <ArticleForm initial={article} onSubmit={handleSubmit} submitLabel="Save Changes" />
    </div>
  );
}
