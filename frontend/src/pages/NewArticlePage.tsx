import { useNavigate } from 'react-router-dom';
import ArticleForm from '../components/ArticleForm';
import { articleService } from '../services/articleService';
import type { CreateArticleRequest } from '../types';

export default function NewArticlePage() {
  const navigate = useNavigate();

  async function handleSubmit(data: CreateArticleRequest) {
    const article = await articleService.create(data);
    navigate(`/articles/${article.id}`);
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>New Article</h1>
      </div>
      <ArticleForm onSubmit={handleSubmit} submitLabel="Create Article" />
    </div>
  );
}
