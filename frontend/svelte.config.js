import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		adapter: adapter({
			// 输出到 .NET 的 wwwroot 目录
			pages: '../src/FlowWorker.Api/wwwroot',
			assets: '../src/FlowWorker.Api/wwwroot',
			fallback: 'index.html'
		})
	}
};

export default config;