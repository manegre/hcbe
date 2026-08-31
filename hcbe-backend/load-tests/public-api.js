import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || 'http://localhost:8080').replace(/\/$/, '');

export const options = {
  stages: [
    { duration: '30s', target: 25 },
    { duration: '2m', target: 100 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<750', 'p(99)<1500'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  const responses = http.batch([
    ['GET', `${baseUrl}/health/ready`],
    ['GET', `${baseUrl}/api/partners`],
    ['GET', `${baseUrl}/api/events`],
    ['GET', `${baseUrl}/api/news`],
  ]);

  responses.forEach((response) => {
    check(response, {
      'request returned 2xx': (result) => result.status >= 200 && result.status < 300,
    });
  });
  sleep(1);
}
