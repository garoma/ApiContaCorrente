SELECT 
  assunto,
  ano,
  COUNT(*) AS quantidade
FROM 
  atendimentos
GROUP BY 
  assunto, ano
HAVING 
  COUNT(*) > 3
ORDER BY 
  ano DESC,
  quantidade DESC;
